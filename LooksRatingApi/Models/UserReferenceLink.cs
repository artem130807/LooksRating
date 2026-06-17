using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Services.UserReferenceLinkService;

namespace LooksRatingApi.Models
{
    public class UserReferenceLink
    {
        public Guid Id {get; private set;}
        public string Link {get; private set;}
        public int CountInvited {get; private set;}
        public Guid UserId {get; private set;}
        public User User {get; private set;}
        public DateTime DateTime {get; private set;} = DateTime.UtcNow;

        public static Result<UserReferenceLink> Create(Guid userId)
        {
            var userReference = new UserReferenceLink
            {
                Id = Guid.NewGuid(),
                Link = GenerateReferenceLink.GenerateLink(userId),
                CountInvited = 0,
                UserId = userId,
                DateTime = DateTime.UtcNow
            };
            return userReference;
        }
        public void AddCountInvited() => CountInvited++;

        public void RemoveCountInvited()
        {
            if (CountInvited > 0)
            {
                CountInvited--;
            }
        }
    }
}