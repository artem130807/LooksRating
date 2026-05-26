using LooksRatingApi.Models;

namespace LooksRatingApi.CQRS.Seasons
{
    internal static class SeasonCatalogMapping
    {
        public static ListSeasonResponse ToListSeasonResponse(Models.ListSeasons list, bool includeSeasons, IReadOnlyDictionary<Guid, int>? photoCounts = null)
        {
            var seasons = list.Seasons?.OrderBy(s => s.Number).ToList() ?? [];
            return new ListSeasonResponse
            {
                Id = list.Id,
                CreatedDate = list.CreatedDate,
                SeasonsCount = seasons.Count,
                Seasons = includeSeasons
                    ? seasons.Select(s => ToSeasonSummary(s, photoCounts)).ToList()
                    : null
            };
        }

        public static SeasonResponse ToSeasonResponse(Season season, int? photoUsersCount = null) =>
            new()
            {
                Id = season.Id,
                Name = season.Name,
                Number = season.Number,
                IsClosed = season.IsClosed,
                ListSeasonsId = season.ListSeasonsId,
                CreatedDate = season.CreatedDate,
                PhotoUsersCount = photoUsersCount ?? 0
            };

        public static SeasonSummaryResponse ToSeasonSummary(Season season, IReadOnlyDictionary<Guid, int>? photoCounts = null) =>
            new()
            {
                Id = season.Id,
                Name = season.Name,
                Number = season.Number,
                IsClosed = season.IsClosed,
                CreatedDate = season.CreatedDate,
                PhotoUsersCount = photoCounts?.GetValueOrDefault(season.Id) ?? 0
            };
    }

    public sealed class ListSeasonResponse
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public int SeasonsCount { get; set; }
        public List<SeasonSummaryResponse>? Seasons { get; set; }
    }

    public sealed class SeasonSummaryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Number { get; set; }
        public bool IsClosed { get; set; }
        public DateTime CreatedDate { get; set; }
        public int PhotoUsersCount { get; set; }
    }

    public sealed class SeasonResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Number { get; set; }
        public bool IsClosed { get; set; }
        public Guid ListSeasonsId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int PhotoUsersCount { get; set; }
        public ListSeasonResponse? Chapter { get; set; }
    }
}
