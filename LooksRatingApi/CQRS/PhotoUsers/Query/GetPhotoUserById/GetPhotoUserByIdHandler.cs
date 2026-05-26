using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.DtoModels.ValueObjectDto;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetPhotoUserById
{
    public class GetPhotoUserByIdHandler : IRequestHandler<GetPhotoUserByIdQuery, Result<GetPhotoUserByIdResponse>>
    {
        private readonly IPhotoUserRepository _photoUserRepository;
        public GetPhotoUserByIdHandler(IPhotoUserRepository photoUserRepository)
        {
            _photoUserRepository = photoUserRepository;
        }
        public async Task<Result<GetPhotoUserByIdResponse>> Handle(GetPhotoUserByIdQuery query, CancellationToken cancellationToken)
        {
            var photo = await _photoUserRepository.GePhotoUserById(query.Id);
            if(photo == null)
                return Result.Failure<GetPhotoUserByIdResponse>("Фото не найдено");
            var rank = RankDisplay.GetSticker(photo.Rank);
            var result = new GetPhotoUserByIdResponse
            {
                Id = photo.Id,
                Image = photo.TelegramFileId,
                UserName = UserPublicDisplayName.Resolve(photo.User),
                Rating = photo.Rating,
                RatingCount = photo.RatingCount,
                Rank = rank,
                Gender = GenderDisplay.GetGender(photo.GenderNomination),
                Age = photo.AgeNomination,
                City = photo.CityNomination.Value
            };
            return result;
        }
    }
}