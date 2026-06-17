using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.AdminModeration;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingApi.CQRS.Users.Command.DeleteUserAccount;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services.CityServices;
using MediatR;

namespace LooksRatingApi.Services
{
    public sealed class AdminTicketModerationService
    {
        private readonly IUserTicketRepository _userTicketRepository;
        private readonly IPhotoProfileModerationService _moderationService;
        private readonly ICityService _cityService;
        private readonly IMediator _mediator;
        private readonly ILogger<AdminTicketModerationService> _logger;

        public AdminTicketModerationService(
            IUserTicketRepository userTicketRepository,
            IPhotoProfileModerationService moderationService,
            ICityService cityService,
            IMediator mediator,
            ILogger<AdminTicketModerationService> logger)
        {
            _userTicketRepository = userTicketRepository;
            _moderationService = moderationService;
            _cityService = cityService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<List<string>> ListModerationCitiesAsync(CancellationToken cancellationToken = default)
        {
            return await _userTicketRepository.GetCitiesWithPendingTickets();
        }

        public async Task<Result<ModerationTicketsByCityResponse>> ListTicketsByCityAsync(
            string city,
            int offset,
            int limit,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return Result.Failure<ModerationTicketsByCityResponse>("Город не указан");
            }

            var resolvedCity = await ResolveQueuedCityAsync(city.Trim(), cancellationToken);
            if (resolvedCity is null)
            {
                return Result.Failure<ModerationTicketsByCityResponse>("Некорректный город");
            }

            var pageSize = limit <= 0 ? 100 : Math.Min(limit, 500);
            var totalCount = await _userTicketRepository.CountTicketsByProfileCity(resolvedCity);
            if (totalCount == 0 || offset >= totalCount)
            {
                return new ModerationTicketsByCityResponse
                {
                    TotalCount = totalCount,
                    Tickets = [],
                };
            }

            var tickets = await _userTicketRepository.GetTicketsByProfileCity(resolvedCity, offset, pageSize);

            return new ModerationTicketsByCityResponse
            {
                TotalCount = totalCount,
                Tickets = tickets.Select(MapSummary).ToList(),
            };
        }

        private async Task<string?> ResolveQueuedCityAsync(string city, CancellationToken cancellationToken)
        {
            if (await _userTicketRepository.CountTicketsByProfileCity(city) > 0)
            {
                return city;
            }

            if (_cityService.TryResolveCanonicalCity(city, out var canonicalCity)
                && await _userTicketRepository.CountTicketsByProfileCity(canonicalCity) > 0)
            {
                return canonicalCity;
            }

            var pendingCities = await _userTicketRepository.GetCitiesWithPendingTickets();

            var directMatch = pendingCities.FirstOrDefault(
                pendingCity => string.Equals(pendingCity, city, StringComparison.OrdinalIgnoreCase));
            if (directMatch is not null)
            {
                return directMatch;
            }

            if (_cityService.TryResolveCanonicalCity(city, out canonicalCity))
            {
                return pendingCities.FirstOrDefault(
                    pendingCity => string.Equals(pendingCity, canonicalCity, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        public async Task<Result<ModerationTicketCountResponse>> CountQueuedTicketsAsync(
            string city,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return Result.Failure<ModerationTicketCountResponse>("Город не указан");
            }

            var resolvedCity = await ResolveQueuedCityAsync(city.Trim(), cancellationToken);
            if (resolvedCity is null)
            {
                return Result.Failure<ModerationTicketCountResponse>("Некорректный город");
            }

            var totalCount = await _userTicketRepository.CountTicketsByProfileCity(resolvedCity);
            return new ModerationTicketCountResponse
            {
                ResolvedCity = resolvedCity,
                TotalCount = totalCount,
            };
        }

        public async Task<Result<ModerationQueuedTicketResponse>> GetQueuedTicketAsync(
            string city,
            int offset,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return Result.Failure<ModerationQueuedTicketResponse>("Город не указан");
            }

            if (offset < 0)
            {
                offset = 0;
            }

            var resolvedCity = await ResolveQueuedCityAsync(city.Trim(), cancellationToken);
            if (resolvedCity is null)
            {
                return Result.Failure<ModerationQueuedTicketResponse>("Некорректный город");
            }

            var totalCount = await _userTicketRepository.CountTicketsByProfileCity(resolvedCity);
            if (totalCount == 0 || offset >= totalCount)
            {
                return new ModerationQueuedTicketResponse
                {
                    ResolvedCity = resolvedCity,
                    TotalCount = totalCount,
                    Offset = offset,
                    Ticket = null,
                };
            }

            var ticketId = await _userTicketRepository.GetTicketIdAtOffsetByProfileCity(resolvedCity, offset);
            if (ticketId is null)
            {
                return new ModerationQueuedTicketResponse
                {
                    ResolvedCity = resolvedCity,
                    TotalCount = totalCount,
                    Offset = offset,
                    Ticket = null,
                };
            }

            var ticket = await _userTicketRepository.GetTicketById(ticketId.Value);
            if (ticket is null)
            {
                return new ModerationQueuedTicketResponse
                {
                    ResolvedCity = resolvedCity,
                    TotalCount = totalCount,
                    Offset = offset,
                    Ticket = null,
                };
            }

            return new ModerationQueuedTicketResponse
            {
                ResolvedCity = resolvedCity,
                TotalCount = totalCount,
                Offset = offset,
                Ticket = MapDetail(ticket),
            };
        }

        public async Task<Result<ModerationTicketDetailDto>> GetTicketDetailAsync(
            string ticketId,
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(ticketId, out var parsedTicketId))
            {
                return Result.Failure<ModerationTicketDetailDto>("Некорректный идентификатор жалобы");
            }

            var ticket = await _userTicketRepository.GetTicketById(parsedTicketId);
            if (ticket is null)
            {
                return Result.Failure<ModerationTicketDetailDto>("Жалоба не найдена");
            }

            return MapDetail(ticket);
        }

        public Task<Result> DismissTicketAsync(
            string ticketId,
            long adminTelegramId,
            CancellationToken cancellationToken = default)
        {
            return ExecuteTicketActionAsync(ticketId, adminTelegramId, _moderationService.DismissTicketAsync, cancellationToken);
        }

        public Task<Result> DeleteReportedProfileAsync(
            string ticketId,
            long adminTelegramId,
            CancellationToken cancellationToken = default)
        {
            return ExecuteTicketActionAsync(ticketId, adminTelegramId, _moderationService.DeleteReportedProfileAsync, cancellationToken);
        }

        public async Task<Result> DeleteReportedUserAccountAsync(
            string ticketId,
            long adminTelegramId,
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(ticketId, out var parsedTicketId))
            {
                return Result.Failure("Некорректный идентификатор жалобы");
            }

            var ticket = await _userTicketRepository.GetTicketById(parsedTicketId);
            if (ticket is null)
            {
                return Result.Failure("Жалоба не найдена");
            }

            var reportedTelegramId = ticket.PhotoProfile?.User?.TelegramId ?? 0;
            if (reportedTelegramId <= 0)
            {
                return Result.Failure("Не удалось определить аккаунт нарушителя");
            }

            var deleteResult = await _mediator.Send(
                new DeleteUserAccountCommand(reportedTelegramId),
                cancellationToken);
            if (deleteResult.IsFailure)
            {
                return Result.Failure(deleteResult.Error);
            }

            _logger.LogInformation(
                "DeleteReportedUserAccount ok ticket={TicketId} reportedTelegramId={ReportedTelegramId} admin={AdminTelegramId}",
                parsedTicketId,
                reportedTelegramId,
                adminTelegramId);

            return Result.Success();
        }

        private static async Task<Result> ExecuteTicketActionAsync(
            string ticketId,
            long adminTelegramId,
            Func<Guid, long, CancellationToken, Task<Result>> action,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(ticketId, out var parsedTicketId))
            {
                return Result.Failure("Некорректный идентификатор жалобы");
            }

            return await action(parsedTicketId, adminTelegramId, cancellationToken);
        }

        private static ModerationTicketSummaryDto MapSummary(UserTicket ticket)
        {
            var profile = ticket.PhotoProfile;
            return new ModerationTicketSummaryDto
            {
                TicketId = ticket.Id.ToString(),
                PhotoProfileId = ticket.PhotoProfileId.ToString(),
                Description = ticket.Description,
                OccuredAtUnix = new DateTimeOffset(ticket.OccuredAt.ToUniversalTime()).ToUnixTimeSeconds(),
                ReporterTelegramId = ticket.User.TelegramId,
                ReporterDisplayName = AdminModerationUserIdentity.FormatLabel(ticket.User),
                ProfileCity = profile.CityNomination.Value ?? string.Empty,
                ProfileTelegramId = profile.User.TelegramId,
                ProfileDisplayName = AdminModerationUserIdentity.FormatLabel(profile.User),
                ProfileAge = profile.AgeNomination,
                ProfileGender = FormatGender(profile.GenderNomination),
                ProfileRating = (double)profile.Rating,
                ProfileRatingCount = profile.RatingCount,
                ProfileRank = profile.Rank.ToString(),
            };
        }

        private static ModerationTicketDetailDto MapDetail(UserTicket ticket)
        {
            var profile = ticket.PhotoProfile;
            return new ModerationTicketDetailDto
            {
                TicketId = ticket.Id.ToString(),
                Description = ticket.Description,
                OccuredAtUnix = new DateTimeOffset(ticket.OccuredAt.ToUniversalTime()).ToUnixTimeSeconds(),
                ReporterTelegramId = ticket.User.TelegramId,
                ReporterDisplayName = AdminModerationUserIdentity.FormatLabel(ticket.User),
                ReporterCity = ticket.User.RecomendationSettings?.City.Value ?? string.Empty,
                PhotoProfileId = ticket.PhotoProfileId.ToString(),
                ProfileTelegramId = profile.User.TelegramId,
                ProfileDisplayName = AdminModerationUserIdentity.FormatLabel(profile.User),
                ProfileCity = profile.CityNomination.Value ?? string.Empty,
                ProfileAge = profile.AgeNomination,
                ProfileGender = FormatGender(profile.GenderNomination),
                ProfileRating = (double)profile.Rating,
                ProfileRatingCount = profile.RatingCount,
                ProfileRank = profile.Rank.ToString(),
                Photos = profile.Photos
                    .OrderBy(photo => photo.SortOrder)
                    .Select(photo => new ModerationTicketPhotoDto
                    {
                        PhotoId = photo.Id.ToString(),
                        TelegramFileId = photo.TelegramFileId,
                        SortOrder = photo.SortOrder,
                    })
                    .ToList(),
            };
        }

        private static string FormatGender(GenderEnum gender) => gender switch
        {
            GenderEnum.Male => "Мужской",
            GenderEnum.Female => "Женский",
            GenderEnum.MaleFamale => "Оба",
            _ => "Не указан",
        };
    }
}
