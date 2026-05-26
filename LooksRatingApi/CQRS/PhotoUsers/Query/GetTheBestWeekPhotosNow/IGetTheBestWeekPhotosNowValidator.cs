using CSharpFunctionalExtensions;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosNow
{
    public interface IGetTheBestWeekPhotosNowValidator
    {
        Task<Result<GetTheBestWeekPhotosNowValidatedContext>> ValidateAsync(
            GetTheBestWeekPhotosNowQuery query,
            CancellationToken cancellationToken);
    }
}
