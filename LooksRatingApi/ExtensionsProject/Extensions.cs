using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi
{
    public static class Extensions
    {
        public static void AddDb(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<LooksRatingDbContext>(service =>
            {
                service.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });
        }
    }
}