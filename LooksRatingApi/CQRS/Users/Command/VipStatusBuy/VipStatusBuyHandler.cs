using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserContracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

namespace LooksRatingApi.CQRS.Users.Command.VipStatusBuy
{
    public class VipStatusBuyHandler : IRequestHandler<VipStatusBuyQuery, Result<VipStatusBuyResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly LooksRatingDbContext _context;
        private readonly ILogger<VipStatusBuyHandler> _logger;
        public VipStatusBuyHandler(IUserRepository userRepository, LooksRatingDbContext context, ILogger<VipStatusBuyHandler> logger)
        {
            _userRepository = userRepository;
            _context = context;
            _logger = logger;
        }
        public async Task<Result<VipStatusBuyResponse>> Handle(VipStatusBuyQuery command, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByTelegramId(command.telegramId);
            if(user != null)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    user.UpdateVipStatus();
                    await _userRepository.SaveChangesAsync();
                    await transaction.CommitAsync(cancellationToken);
                }catch(Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Ошибка обновления VIP-статуса");
                    return new VipStatusBuyResponse{Message = "Статус не обновлен", Result = false};
                }
                return new VipStatusBuyResponse{Message = "Статус успешно обновлен", Result = true};
            }
            return new VipStatusBuyResponse{Message = "Статус не обновлен", Result = false};
            
        }
    }
}