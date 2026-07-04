using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public sealed class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<CreateReviewResult>>
    {
        private readonly LooksRatingDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly ICreateReviewValidator _validator;
        private readonly IKafkaPhotoRatedProducer<PhotoRatedEvent> _producer;
        private readonly ICreateReviewEventPublisher _createReviewEventPublisher;
        private readonly IRankService _rankService;
        private readonly IPhotoRatingCacheService _photoRatingCacheService;
        private readonly IReviewSparksRewardService _reviewSparksRewardService;
        private readonly IRatedProfileSparksRewardService _ratedProfileSparksRewardService;
        private readonly IAddLastActiveUser _addLastActiveUser;
        private readonly ILogger<CreateReviewCommandHandler> _logger;

        public CreateReviewCommandHandler(
            LooksRatingDbContext context,
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            IReviewRepository reviewRepository,
            ICreateReviewValidator validator,
            IKafkaPhotoRatedProducer<PhotoRatedEvent> producer,
            ICreateReviewEventPublisher createReviewEventPublisher,
            IRankService rankService,
            IPhotoRatingCacheService photoRatingCacheService,
            IReviewSparksRewardService reviewSparksRewardService,
            IRatedProfileSparksRewardService ratedProfileSparksRewardService,
            IAddLastActiveUser addLastActiveUser,
            ILogger<CreateReviewCommandHandler> logger)
        {
            _context = context;
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _reviewRepository = reviewRepository;
            _validator = validator;
            _producer = producer;
            _createReviewEventPublisher = createReviewEventPublisher;
            _rankService = rankService;
            _photoRatingCacheService = photoRatingCacheService;
            _reviewSparksRewardService = reviewSparksRewardService;
            _ratedProfileSparksRewardService = ratedProfileSparksRewardService;
            _addLastActiveUser = addLastActiveUser;
            _logger = logger;
        }

        public async Task<Result<CreateReviewResult>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<CreateReviewResult>(validationResult.Error);
            }

            var reviewer = await _userRepository.GetUserByTelegramId(request.ReviewerTelegramId);
            if (reviewer is null)
            {
                return Result.Failure<CreateReviewResult>(CreateReviewErrors.ReviewerNotFound);
            }

            var photoProfile = await _photoProfileRepository.GetByIdAsync(request.PhotoProfileId, cancellationToken);
            if (photoProfile is null)
            {
                return Result.Failure<CreateReviewResult>(CreateReviewErrors.PhotoProfileNotFound);
            }

            Review review;
            var isNewReview = false;
            PhotoRatedEvent? domainEvent = null;
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var existingReview = await _reviewRepository.GetByUserAndProfileAsync(
                    reviewer.Id,
                    photoProfile.Id,
                    cancellationToken);

                if (existingReview is null)
                {
                    var reviewResult = Review.Create(request.Rating, reviewer.Id, photoProfile.Id);
                    if (reviewResult.IsFailure)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<CreateReviewResult>(reviewResult.Error);
                    }

                    review = reviewResult.Value;
                    photoProfile.AddRating(request.Rating);
                    _context.Reviews.Add(review);
                    isNewReview = true;
                }
                else
                {
                    var previousRating = existingReview.Rating;
                    var updateResult = existingReview.UpdateRating(request.Rating);
                    if (updateResult.IsFailure)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<CreateReviewResult>(updateResult.Error);
                    }

                    review = existingReview;
                    photoProfile.ChangeRating(previousRating, request.Rating);
                    _context.Reviews.Update(review);
                }

                var rank = _rankService.GetRankEnum(photoProfile.Rating);
                photoProfile.UpdateRank(rank);

                var city = photoProfile.CityNomination?.Value ?? string.Empty;
                domainEvent = new PhotoRatedEvent(
                    photoProfile.Id,
                    photoProfile.Rating,
                    photoProfile.RatingCount,
                    city,
                    photoProfile.SeasonId);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueReviewConflict(ex))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<CreateReviewResult>(CreateReviewErrors.ReviewAlreadyExists);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(
                    ex,
                    "Failed to persist review for profile {PhotoProfileId} by reviewer {ReviewerTelegramId}",
                    request.PhotoProfileId,
                    request.ReviewerTelegramId);
                return Result.Failure<CreateReviewResult>(CreateReviewErrors.InternalError);
            }

            if (domainEvent is not null)
            {
                try
                {
                    await _photoRatingCacheService.MarkProfileAsRatedAsync(
                        reviewer.Id,
                        photoProfile.SeasonId,
                        photoProfile.Id,
                        cancellationToken);
                    await _photoRatingCacheService.SyncPhotoRatingAsync(domainEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to sync review cache for profile {PhotoProfileId}",
                        photoProfile.Id);
                }

                try
                {
                    await _producer.ProduceAsync(domainEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to publish PhotoRatedEvent for profile {PhotoProfileId}",
                        photoProfile.Id);
                }
            }

            try
            {
                if (isNewReview)
                {
                    await _createReviewEventPublisher.PublishAsync(
                        reviewer.Id,
                        photoProfile.Id,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish CreateReviewEvent for profile {PhotoProfileId}", photoProfile.Id);
            }

            await _reviewSparksRewardService.TryAwardForReviewAsync(
                reviewer.TelegramId,
                reviewer.Id,
                cancellationToken);

            if (photoProfile.User is not null)
            {
                await _ratedProfileSparksRewardService.TryAwardForRatedProfileAsync(
                    photoProfile.User.TelegramId,
                    photoProfile.UserId,
                    cancellationToken);
            }
            else
            {
                _logger.LogWarning(
                    "Skipping rated-profile sparks reward because profile {PhotoProfileId} has no loaded owner",
                    photoProfile.Id);
            }

            try
            {
                await _addLastActiveUser.Add(reviewer.Id, reviewer.TelegramId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to update last active timestamp for reviewer {ReviewerId}",
                    reviewer.Id);
            }

            return Result.Success(new CreateReviewResult
            {
                ReviewId = review.Id,
                ReviewerUserId = reviewer.Id,
                PhotoProfileId = photoProfile.Id,
                Rating = request.Rating,
                UpdatedProfileRating = photoProfile.Rating,
                UpdatedProfileRatingCount = photoProfile.RatingCount
            });
        }

        private static bool IsUniqueReviewConflict(DbUpdateException exception)
        {
            if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }

            var message = exception.InnerException?.Message ?? exception.Message;
            return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
