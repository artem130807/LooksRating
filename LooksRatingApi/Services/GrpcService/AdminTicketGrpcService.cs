using Grpc.Core;
using LooksRatingApi.Contracts;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Grpc;
using LooksRatingApi.Models;
using LooksRatingGrpc;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Services.GrpcService
{
    public sealed class AdminTicketGrpcService : AdminTicketService.AdminTicketServiceBase
    {
        private readonly AdminTicketModerationService _moderationService;
        private readonly IOptions<ApiKeyAuthOptions> _apiKeyOptions;
        private readonly ILogger<AdminTicketGrpcService> _logger;

        public AdminTicketGrpcService(
            AdminTicketModerationService moderationService,
            IOptions<ApiKeyAuthOptions> apiKeyOptions,
            ILogger<AdminTicketGrpcService> logger)
        {
            _moderationService = moderationService;
            _apiKeyOptions = apiKeyOptions;
            _logger = logger;
        }

        public override async Task<ListModerationCitiesResponse> ListModerationCities(
            ListModerationCitiesRequest request,
            ServerCallContext context)
        {
            GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);

            try
            {
                var cities = await _moderationService.ListModerationCitiesAsync(context.CancellationToken);
                var response = new ListModerationCitiesResponse();
                response.Cities.AddRange(cities);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListModerationCities failed");
                throw new RpcException(new Status(StatusCode.Internal, "Не удалось загрузить список городов"));
            }
        }

        public override async Task<ListTicketsByCityResponse> ListTicketsByCity(
            ListTicketsByCityRequest request,
            ServerCallContext context)
        {
            GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);

            var result = await _moderationService.ListTicketsByCityAsync(
                request.City,
                request.Offset,
                request.Limit,
                context.CancellationToken);
            if (result.IsFailure)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Error));
            }

            var response = new ListTicketsByCityResponse
            {
                TotalCount = result.Value.TotalCount,
            };

            foreach (var ticket in result.Value.Tickets)
            {
                response.Tickets.Add(MapSummary(ticket));
            }

            return response;
        }

        public override async Task<TicketDetail> GetTicketDetail(
            GetTicketDetailRequest request,
            ServerCallContext context)
        {
            GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);

            var result = await _moderationService.GetTicketDetailAsync(request.TicketId, context.CancellationToken);
            if (result.IsFailure)
            {
                var status = result.Error.Contains("Некорректный", StringComparison.Ordinal)
                    ? StatusCode.InvalidArgument
                    : StatusCode.NotFound;
                throw new RpcException(new Status(status, result.Error));
            }

            return MapDetail(result.Value);
        }

        public override async Task<DismissTicketResponse> DismissTicket(
            DismissTicketRequest request,
            ServerCallContext context)
        {
            GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);

            var result = await _moderationService.DismissTicketAsync(
                request.TicketId,
                request.AdminTelegramId,
                context.CancellationToken);
            if (result.IsFailure)
            {
                throw new RpcException(new Status(StatusCode.NotFound, result.Error));
            }

            _logger.LogInformation(
                "DismissTicket grpc ok ticket={TicketId} admin={AdminTelegramId}",
                request.TicketId,
                request.AdminTelegramId);

            return new DismissTicketResponse { Success = true };
        }

        public override async Task<DeleteReportedProfileResponse> DeleteReportedProfile(
            DeleteReportedProfileRequest request,
            ServerCallContext context)
        {
            GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);

            var result = await _moderationService.DeleteReportedProfileAsync(
                request.TicketId,
                request.AdminTelegramId,
                context.CancellationToken);
            if (result.IsFailure)
            {
                throw new RpcException(new Status(StatusCode.NotFound, result.Error));
            }

            _logger.LogInformation(
                "DeleteReportedProfile grpc ok ticket={TicketId} admin={AdminTelegramId}",
                request.TicketId,
                request.AdminTelegramId);

            return new DeleteReportedProfileResponse { Success = true };
        }

        private static TicketSummary MapSummary(Contracts.AdminModeration.ModerationTicketSummaryDto ticket)
        {
            return new TicketSummary
            {
                TicketId = ticket.TicketId,
                PhotoProfileId = ticket.PhotoProfileId,
                Description = ticket.Description,
                OccuredAtUnix = ticket.OccuredAtUnix,
                ReporterTelegramId = ticket.ReporterTelegramId,
                ReporterDisplayName = ticket.ReporterDisplayName,
                ProfileCity = ticket.ProfileCity,
                ProfileDisplayName = ticket.ProfileDisplayName,
                ProfileAge = ticket.ProfileAge,
                ProfileGender = ticket.ProfileGender,
                ProfileRating = ticket.ProfileRating,
                ProfileRatingCount = ticket.ProfileRatingCount,
                ProfileRank = ticket.ProfileRank,
            };
        }

        private static TicketDetail MapDetail(Contracts.AdminModeration.ModerationTicketDetailDto ticket)
        {
            var detail = new TicketDetail
            {
                TicketId = ticket.TicketId,
                Description = ticket.Description,
                OccuredAtUnix = ticket.OccuredAtUnix,
                ReporterTelegramId = ticket.ReporterTelegramId,
                ReporterDisplayName = ticket.ReporterDisplayName,
                ReporterCity = ticket.ReporterCity,
                PhotoProfileId = ticket.PhotoProfileId,
                ProfileDisplayName = ticket.ProfileDisplayName,
                ProfileCity = ticket.ProfileCity,
                ProfileAge = ticket.ProfileAge,
                ProfileGender = ticket.ProfileGender,
                ProfileRating = ticket.ProfileRating,
                ProfileRatingCount = ticket.ProfileRatingCount,
                ProfileRank = ticket.ProfileRank,
            };

            foreach (var photo in ticket.Photos)
            {
                detail.Photos.Add(new TicketPhoto
                {
                    PhotoId = photo.PhotoId,
                    TelegramFileId = photo.TelegramFileId,
                    SortOrder = photo.SortOrder,
                });
            }

            return detail;
        }
    }
}
