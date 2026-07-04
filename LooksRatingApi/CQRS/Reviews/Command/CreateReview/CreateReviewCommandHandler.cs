using CSharpFunctionalExtensions;
using Hangfire;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.UserContracts;
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
        private readonly IRankService _rankService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<CreateReviewCommandHandler> _logger;

        public CreateReviewCommandHandler(
            LooksRatingDbContext context,
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            IReviewRepository reviewRepository,
            ICreateReviewValidator validator,
            IRankService rankService,
            IBackgroundJobClient backgroundJobClient,
            ILogger<CreateReviewCommandHandler> logger)
        {
            _context = context;
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _reviewRepository = reviewRepository;
            _validator = validator;
            _rankService = rankService;
            _backgroundJobClient = backgroundJobClient;
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
            var city = string.Empty;
            OutboxMessage? outboxRecord = null;
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

                city = photoProfile.CityNomination?.Value ?? string.Empty;
                var profileOwnerTelegramId = photoProfile.User is null
                    ? (long?)null
                    : photoProfile.User.TelegramId;

                outboxRecord = OutboxMessage.Create(
                    CreateReviewOutboxMessage.Type,
                    new CreateReviewOutboxPayload
                    {
                        ReviewId = review.Id,
                        ReviewerUserId = reviewer.Id,
                        ReviewerTelegramId = reviewer.TelegramId,
                        PhotoProfileId = photoProfile.Id,
                        SeasonId = photoProfile.SeasonId,
                        IsNewReview = isNewReview,
                        UpdatedProfileRating = photoProfile.Rating,
                        UpdatedProfileRatingCount = photoProfile.RatingCount,
                        ProfileCity = city,
                        ProfileOwnerUserId = photoProfile.UserId,
                        ProfileOwnerTelegramId = profileOwnerTelegramId
                    },
                    CreateReviewOutboxState.Initial(isNewReview));
                _context.OutboxMessages.Add(outboxRecord);

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

            try
            {
                _backgroundJobClient.Enqueue<IReviewBackgroundService>(service =>
                    service.ProcessOutboxAsync(outboxRecord!.Id, CancellationToken.None));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to enqueue create-review side effects for profile {PhotoProfileId}",
                    photoProfile.Id);
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
