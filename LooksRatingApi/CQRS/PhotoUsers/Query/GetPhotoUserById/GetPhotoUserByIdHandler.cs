using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetPhotoUserById
{
    public class GetPhotoUserByIdHandler : IRequestHandler<GetPhotoUserByIdQuery, Result<GetPhotoUserByIdResponse>>
    {
        private readonly IPhotoProfileRepository _photoProfileRepository;
        public GetPhotoUserByIdHandler(IPhotoProfileRepository photoProfileRepository)
        {
            _photoProfileRepository = photoProfileRepository;
        }
        public async Task<Result<GetPhotoUserByIdResponse>> Handle(GetPhotoUserByIdQuery query, CancellationToken cancellationToken)
        {
            var profile = await _photoProfileRepository.GetByIdAsync(query.Id, cancellationToken);
            if(profile == null)
                return Result.Failure<GetPhotoUserByIdResponse>("Фото не найдено");
            var rank = RankDisplay.GetSticker(profile.Rank);
            var images = profile.Photos
                .OrderBy(x => x.SortOrder)
                .Select(x => new GetPhotoUserByIdImageResponse
                {
                    Id = x.Id,
                    TelegramFileId = x.TelegramFileId
                })
                .ToList();
            var firstImage = images.FirstOrDefault()?.TelegramFileId ?? string.Empty;
            var result = new GetPhotoUserByIdResponse
            {
                Id = profile.Id,
                ProfileId = profile.Id,
                Image = firstImage,
                Images = images,
                UserName = UserPublicDisplayName.Resolve(profile.User),
                Rating = profile.Rating,
                RatingCount = profile.RatingCount,
                Rank = rank,
                Gender = GenderDisplay.GetGender(profile.GenderNomination),
                Age = profile.AgeNomination,
                City = profile.CityNomination.Value
            };
            return result;
        }
    }
}