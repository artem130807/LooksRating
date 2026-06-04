using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Models;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;

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
        private readonly IRankService _rankService;
        private readonly IPhotoRatingCacheService _photoRatingCacheService;

        public CreateReviewCommandHandler(
            LooksRatingDbContext context,
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            IReviewRepository reviewRepository,
            ICreateReviewValidator validator,
            IKafkaPhotoRatedProducer<PhotoRatedEvent> producer,
            IRankService rankService,
            IPhotoRatingCacheService photoRatingCacheService)
        {
            _context = context;
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _reviewRepository = reviewRepository;
            _validator = validator;
            _producer = producer;
            _rankService = rankService;
            _photoRatingCacheService = photoRatingCacheService;
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

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var existingReview = await _reviewRepository.GetByUserAndProfileAsync(
                    reviewer.Id,
                    photoProfile.Id,
                    cancellationToken);

                Review review;
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
                    await _reviewRepository.Create(review);
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
                    await _reviewRepository.Update(review);
                }

                var rank = _rankService.GetRankEnum(photoProfile.Rating);
                photoProfile.UpdateRank(rank);
                await _photoProfileRepository.UpdateAsync(photoProfile, cancellationToken);

                var city = photoProfile.CityNomination.Value ?? string.Empty;
                var domainEvent = new PhotoRatedEvent(
                    photoProfile.Id,
                    photoProfile.Rating,
                    photoProfile.RatingCount,
                    city,
                    photoProfile.SeasonId);

                await _photoRatingCacheService.MarkProfileAsRatedAsync(
                    reviewer.Id,
                    photoProfile.SeasonId,
                    photoProfile.Id,
                    cancellationToken);
                await _photoRatingCacheService.SyncPhotoRatingAsync(domainEvent, cancellationToken);
                await _producer.ProduceAsync(domainEvent, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

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
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
