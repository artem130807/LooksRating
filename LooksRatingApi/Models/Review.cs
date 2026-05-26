using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace LooksRatingApi.Models
{
    public class Review
    {
        private const int MinRating = 1;
        private const int MaxRating = 10;
        public Guid Id { get; private set; }
        public int Rating { get; private set; }
        public Guid UserId { get; private set; }
        public User User { get; private set; } = null!;
        public Guid PhotoUserId { get; private set; }
        public PhotoUser PhotoUser { get; private set; } = null!;

        public static Result<Review> Create(int rating, Guid userId, Guid photoUserId)
        {
            if (userId == Guid.Empty)
                return Result.Failure<Review>("ReviewUserIdIsRequired");

            if (photoUserId == Guid.Empty)
                return Result.Failure<Review>("ReviewPhotoUserIdIsRequired");

            if (rating is < MinRating or > MaxRating)
                return Result.Failure<Review>("ReviewRatingIsOutOfRange");

            var review = new Review
            {
               Id = Guid.NewGuid(),
               Rating = rating,
               UserId = userId, 
               PhotoUserId = photoUserId  
            };
            return review;
        }

        public Result UpdateRating(int rating)
        {
            if (rating is < MinRating or > MaxRating)
                return Result.Failure("ReviewRatingIsOutOfRange");

            Rating = rating;
            return Result.Success();
        }
    }
}