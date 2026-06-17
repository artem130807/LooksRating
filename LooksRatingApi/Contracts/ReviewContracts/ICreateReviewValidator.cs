using CSharpFunctionalExtensions;
using LooksRatingApi.Cqrs.Reviews.Command.CreateReview;

namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public interface ICreateReviewValidator
    {
        Task<Result<string>> ValidateAsync(CreateReviewCommand command, CancellationToken cancellationToken);
    }
}
