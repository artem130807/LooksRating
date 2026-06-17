using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Models
{
    public class TheBestWeek
    {
        public Guid Id { get; set; }
        public string City { get; set; } = string.Empty;
        public int Year { get; set; }
        public int WeekOfYear { get; set; }
        public WeekEnum Week { get; set; }
        public string SnapshotJson { get; set; }
        public DateTime CreatedDate { get; set; }

        public static Result<TheBestWeek> Create(string city, int year, int weekOfYear, WeekEnum week, string snapshotJson)
        {
            if (string.IsNullOrWhiteSpace(city))
                return Result.Failure<TheBestWeek>("Город обязателен");

            return new TheBestWeek
            {
                Id = Guid.NewGuid(),
                City = city.Trim(),
                Year = year,
                SnapshotJson = snapshotJson,
                WeekOfYear = weekOfYear,
                Week = week,
                CreatedDate = DateTime.UtcNow
            };
        }
    }
}
