using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Services.SparksWallet;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.Orchestrators
{
    public class DebitedSparksOrchestrator : IDebitedSparksOrchestrator
    {
        private readonly ICurrencyDebitedService _currencyDebitedService;
        private readonly ILogger<DebitedSparksOrchestrator> _logger;
        private readonly LooksRatingDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly ISparksLedgerRepository _sparksLedgerRepository;
        public DebitedSparksOrchestrator(
            ICurrencyDebitedService currencyDebitedService,
            ILogger<DebitedSparksOrchestrator> logger,
            LooksRatingDbContext context,
            IUserRepository userRepository,
            ISparksLedgerRepository sparksLedgerRepository)
        {
            _currencyDebitedService = currencyDebitedService;
            _logger = logger;
            _context = context;
            _userRepository = userRepository;
            _sparksLedgerRepository = sparksLedgerRepository;
        }

        public async Task<Result<DebitedSparksResponse>> DebitedSparks(long telegramId, int starsCount, CancellationToken cancellationToken)
        { 
            var user = await _userRepository.GetUserByTelegramId(telegramId);
            if(user == null)
                return Result.Success(new DebitedSparksResponse{Success = false, Message = "Пользователь не найден"});
            if(user.Status == Enums.VipStatus.Unavaillable)
                return Result.Success(new DebitedSparksResponse{Success = false, Message = "Для начала нужно приобрести вип статус"});
            if (!SparksGiftExchangeRules.TryGetSparksCost(starsCount, out var sparks))
                return Result.Success(new DebitedSparksResponse{Success = false, Message = "Недопустимая стоимость подарка"});
            var balance = await _sparksLedgerRepository.GetBalanceAsync(user.Id, cancellationToken);
            var remainder = balance - sparks;
            if(remainder < 0)
                return Result.Success(new DebitedSparksResponse{Success = false, Message = "Недостаточно искр на балансе"});
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _currencyDebitedService.Debited(user.Id, sparks, cancellationToken);
                await _sparksLedgerRepository.SaveChangesAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }catch(Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogInformation(ex.Message);
                return Result.Success(new DebitedSparksResponse{Success = false, Message = ex.Message});
            }
            return Result.Success(new DebitedSparksResponse{Success = true, Message = "Успешно"});
        }
    }
}