using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Infrastructure.Quartz
{
    public sealed class TestQuartzQuartzOptionsConfigurer : IConfigureOptions<LooksRatingQuartzOptions>
    {
        private readonly IHostEnvironment _environment;

        public TestQuartzQuartzOptionsConfigurer(IHostEnvironment environment)
        {
            _environment = environment;
        }

        public void Configure(LooksRatingQuartzOptions options)
        {
            if (!_environment.IsEnvironment("TestQuartz") || !options.MirrorProductionCron)
            {
                return;
            }

            options.TimeZoneId = "Europe/Moscow";
            options.VipStatusExpiryCron = "0 0 * * * ?";
            options.TheBestWeekCron = "0 0 0 ? * MON";
            options.NewSeasonCron = "0 0 0 1 2-12 ?";
            options.NewListSeasonCron = "0 0 0 1 1 ?";
        }
    }
}
