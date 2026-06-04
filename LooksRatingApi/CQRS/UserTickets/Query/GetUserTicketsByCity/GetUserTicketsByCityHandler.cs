using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.UserTicketContracts;
using MediatR;

namespace LooksRatingApi.CQRS.UserTickets.Query.GetUserTicketsByCity
{
    public sealed class GetUserTicketsByCityHandler
        : IRequestHandler<GetUserTicketsByCityQuery, Result<List<GetUserTicketsByCityResponse>>>
    {
        private readonly IUserTicketRepository _userTicketRepository;
        private readonly ICityService _cityService;
        private readonly INormalizeCityNameService _normalizeCityNameService;

        public GetUserTicketsByCityHandler(
            IUserTicketRepository userTicketRepository,
            ICityService cityService,
            INormalizeCityNameService normalizeCityNameService)
        {
            _userTicketRepository = userTicketRepository;
            _cityService = cityService;
            _normalizeCityNameService = normalizeCityNameService;
        }

        public async Task<Result<List<GetUserTicketsByCityResponse>>> Handle(
            GetUserTicketsByCityQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.City))
            {
                return Result.Failure<List<GetUserTicketsByCityResponse>>("Город не указан");
            }

            var normalizedCity = _normalizeCityNameService.Normalize(request.City);
            if (!_cityService.IsCityValid(normalizedCity))
            {
                return Result.Failure<List<GetUserTicketsByCityResponse>>("Некорректный город");
            }

            var tickets = await _userTicketRepository.GetTicketsByUsersCity(normalizedCity);

            var response = tickets.Select(t => new GetUserTicketsByCityResponse
            {
                Id = t.Id,
                Description = t.Description,
                OccuredAt = t.OccuredAt,
                ReporterUserId = t.UserId,
                ReporterTelegramId = t.User.TelegramId,
                PhotoProfileId = t.PhotoProfileId,
                PhotoTelegramFileIds = t.PhotoProfile.Photos
                    .OrderBy(p => p.SortOrder)
                    .Select(p => p.TelegramFileId)
                    .ToList()
            }).ToList();

            return Result.Success(response);
        }
    }
}
