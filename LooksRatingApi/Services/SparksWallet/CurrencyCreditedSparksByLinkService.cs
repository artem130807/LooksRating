using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using Microsoft.Identity.Client;

namespace LooksRatingApi.Services.SparksWallet
{
    public class CurrencyCreditedSparksByLinkService : ICurrencyCreditedSparksByLinkService
    {
        private readonly IUserReferenceLinkRepository _userReferenceLinkRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISparksLedgerRepository _sparksLedgerRepository;
        public CurrencyCreditedSparksByLinkService(IUserReferenceLinkRepository userReferenceLinkRepository,IUserRepository userRepository, ISparksLedgerRepository sparksLedgerRepository)
        {
            _userReferenceLinkRepository = userReferenceLinkRepository;
            _userRepository = userRepository;
            _sparksLedgerRepository = sparksLedgerRepository;
        }
        public async Task Currency(string? linkUserId)
        {
            if (!string.IsNullOrWhiteSpace(linkUserId))
            {
                var userId = Guid.Parse(linkUserId);
                var user = await _userRepository.GetUserById(userId);
                if(user != null)
                {
                    var reference = await _userReferenceLinkRepository.GetByUserId(userId);
                    if(reference != null)
                    {
                        if(reference.CountInvited < 5)
                        {
                            var sparksWallet = await _sparksLedgerRepository.GetSparksByUserId(userId);
                            if(sparksWallet == null)
                            {
                                reference.AddCountInvited();
                                await _userReferenceLinkRepository.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
        }
    }
}