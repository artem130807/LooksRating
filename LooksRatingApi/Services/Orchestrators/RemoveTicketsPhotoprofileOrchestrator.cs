using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingApi.Models;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.Orchestrators
{
    public class RemoveTicketsPhotoprofileOrchestrator : IRemoveTicketsPhotoprofileOrchestrator
    {
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IUserTicketRepository _userTicketRepository;
        private readonly IUnviewablePhotosProfilesService _unviewablePhotosProfilesService;
        private readonly LooksRatingDbContext _context;
        private readonly ILogger<RemoveTicketsPhotoprofileOrchestrator> _logger;

        public RemoveTicketsPhotoprofileOrchestrator(
            IPhotoProfileRepository photoProfileRepository,
            IUserTicketRepository userTicketRepository,
            IUnviewablePhotosProfilesService unviewablePhotosProfilesService,
            LooksRatingDbContext context,
            ILogger<RemoveTicketsPhotoprofileOrchestrator> logger)
        {
            _photoProfileRepository = photoProfileRepository;
            _userTicketRepository = userTicketRepository;
            _unviewablePhotosProfilesService = unviewablePhotosProfilesService;
            _context = context;
            _logger = logger;
        }

        public async Task<Result<RemoveTicketsPhotoprofileResponse>> RemoveTickets(
            Guid photoProfileId,
            CancellationToken cancellationToken)
        {
            var photoProfile = await _photoProfileRepository.GetByIdAsync(photoProfileId);
            if (photoProfile == null)
            {
                return Result.Success(new RemoveTicketsPhotoprofileResponse
                {
                    Message = "Фото профиль не найден",
                    IsSuccess = false,
                });
            }

            var lockToken = await _userTicketRepository.LockPhotoProfileAsync(photoProfileId, cancellationToken);
            if (lockToken is null)
            {
                return Result.Success(new RemoveTicketsPhotoprofileResponse
                {
                    Message = "Удаление занято",
                    IsSuccess = false,
                });
            }

            var tickets = await _userTicketRepository.GetUserTicketsByPhotoProfileId(photoProfileId);
            var reporterUserIds = tickets
                .Select(ticket => ticket.UserId)
                .Distinct()
                .ToList();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _userTicketRepository.RemoveUserTickets(photoProfile.Id);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to remove tickets for photo profile {PhotoProfileId}",
                    photoProfileId);
                await transaction.RollbackAsync(cancellationToken);
                return Result.Success(new RemoveTicketsPhotoprofileResponse
                {
                    Message = "Ошибка удаления жалоб",
                    IsSuccess = false,
                });
            }
            finally
            {
                await _userTicketRepository.UnlockPhotoProfileAsync(photoProfileId, lockToken, cancellationToken);
            }

            var cacheResult = await _unviewablePhotosProfilesService.RemoveUnviewablePhotosProfile(
                photoProfileId,
                reporterUserIds,
                cancellationToken);
            if (cacheResult.IsFailure)
            {
                _logger.LogWarning(
                    "Tickets removed for photo profile {PhotoProfileId}, but unviewable cache cleanup failed: {Error}",
                    photoProfileId,
                    cacheResult.Error);
            }

            return Result.Success(new RemoveTicketsPhotoprofileResponse
            {
                Message = "Успешно",
                IsSuccess = true,
            });
        }
    }
}
