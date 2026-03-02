using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.TheBestWeekContracts
{
    public interface ITheBestWeekRepository
    {
        Task Create(TheBestWeek theBestWeek);
        Task Delete(Guid Id);
        Task Update(TheBestWeek theBestWeek);
       Task<List<TheBestWeek>> GetTheBestWeekByCity(string city);
    }
}