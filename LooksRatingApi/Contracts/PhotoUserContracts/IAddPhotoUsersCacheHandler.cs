using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IAddPhotoUsersCacheHandler
    {
        Task Handle(CancellationToken cancellationToken);
    }
}