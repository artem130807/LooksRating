using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Models;

namespace LooksRatingApi.Services
{
    public class AddCountInTopService : IAddCountInTopService
    {
        private readonly IUserRepository _userRepository;
        public AddCountInTopService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task Handle(List<long> ids)
        {
            
        }
    }
}