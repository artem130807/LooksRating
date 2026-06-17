using LooksRatingApi.Enums;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto
{
    public sealed class PhotoNominationRequest
    {
        public bool UseProfileNomination { get; set; }
        public string? City { get; set; }
        public int? Age { get; set; }
        public GenderEnum? Gender { get; set; }
    }
}
