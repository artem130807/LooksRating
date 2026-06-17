using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Reviews.Query.GetMilestoneReviewers
{
    public sealed record GetMilestoneReviewersQuery(Guid NotificationId)
        : IRequest<Result<GetMilestoneReviewersResponse>>;
}
