using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Command.UpdateGenderUser
{
    public record UpdateGenderUserCommandCommand(long telegramId, GenderEnum gender):IRequest<Result<string>>;
}