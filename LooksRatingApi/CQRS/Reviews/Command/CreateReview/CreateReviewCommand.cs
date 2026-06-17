using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public sealed record CreateReviewCommand(
        long ReviewerTelegramId,
        Guid PhotoProfileId,
        int Rating) : IRequest<Result<CreateReviewResult>>;
}
