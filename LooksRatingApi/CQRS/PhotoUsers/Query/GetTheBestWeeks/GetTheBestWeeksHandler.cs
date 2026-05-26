using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeeks
{
    public sealed class GetTheBestWeeksHandler
        : IRequestHandler<GetTheBestWeeksQuery, Result<List<GetTheBestWeeksResponse>>>
    {
        private readonly ITheBestWeekRepository _theBestWeekRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        public GetTheBestWeeksHandler(
            ITheBestWeekRepository theBestWeekRepository,
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository)
        {
            _theBestWeekRepository = theBestWeekRepository;
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
        }

        public async Task<Result<List<GetTheBestWeeksResponse>>> Handle(
            GetTheBestWeeksQuery query,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByTelegramId(query.TelegramId.Value);
            if(user == null)
                return Result.Failure<List<GetTheBestWeeksResponse>>("Пользователь не найден");
            var currentTheBestWeek = await _theBestWeekRepository.GetCurrentWeek();
            if(currentTheBestWeek == null)
                return Result.Failure<List<GetTheBestWeeksResponse>>("Действующая неделя не найдена");
            var photoUsers = await _photoUserRepository.GetByCityAsync(currentTheBestWeek.Id, user.RecomendationSettings.City.Value, user.RecomendationSettings.Age.Value,
            user.RecomendationSettings.Gender);
            var dtoPhotoUsers = photoUsers.Select(p => new GetTheBestWeeksResponse
            {
                Id = p.Id,
                TelegramUsername = p.User.TelegramUsername,
                Name = p.User.Name,
                Rating = p.Rating,
                RatingCount = p.RatingCount
            }).ToList();
            return dtoPhotoUsers;
        }
    }
}
