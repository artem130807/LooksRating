using LooksRatingApi.Services.SparksWallet;
using MediatR;

namespace LooksRatingApi.CQRS.Payments.Query.GetGiftExchangeRates
{
    public sealed record GetGiftExchangeRatesQuery : IRequest<GiftExchangeRatesResponse>;

    public sealed class GetGiftExchangeRatesHandler
        : IRequestHandler<GetGiftExchangeRatesQuery, GiftExchangeRatesResponse>
    {
        public Task<GiftExchangeRatesResponse> Handle(
            GetGiftExchangeRatesQuery request,
            CancellationToken cancellationToken)
        {
            var response = new GiftExchangeRatesResponse
            {
                SparksPerStar = SparksGiftExchangeRules.SparksPerStar,
                Gifts = SparksGiftExchangeRules.GetRates()
                    .Select(rate => new GiftExchangeRateItem
                    {
                        StarTier = rate.StarTier,
                        SparksCost = rate.SparksCost,
                    })
                    .ToArray(),
            };

            return Task.FromResult(response);
        }
    }
}
