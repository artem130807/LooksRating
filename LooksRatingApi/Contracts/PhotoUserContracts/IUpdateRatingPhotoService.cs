using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IUpdateRatingPhotoService
    {
        Task<Result> Update(List<PhotoRatedEvent> message, CancellationToken cancellationToken);
    }
}