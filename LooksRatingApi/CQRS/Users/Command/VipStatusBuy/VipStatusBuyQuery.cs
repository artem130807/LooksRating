using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Command.VipStatusBuy
{
    public record VipStatusBuyQuery(long telegramId):IRequest<Result<VipStatusBuyResponse>>;
}