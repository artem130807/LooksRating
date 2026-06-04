using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Configurations;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi
{
    public class LooksRatingDbContext(DbContextOptions<LooksRatingDbContext> options): DbContext(options)
    {
        public DbSet<User> Users {get; set;}
        public DbSet<RecomendationSettings> RecomendationSettings {get; set;}
        public DbSet<Review> Reviews {get; set;}
        public DbSet<PhotoUser> PhotoUsers {get; set;}
        public DbSet<PhotoProfile> PhotoProfiles {get; set;}
        public DbSet<PhotoProfilePhoto> PhotoProfilePhotos {get; set;}
        public DbSet<UserSession> UserSessions {get; set;}
        public DbSet<TheBestWeek> TheBestWeeks {get; set;}
        public DbSet<UserTicket> UserTickets {get; set;}
        public DbSet<ListSeasons> ListSeasons {get; set;}
        public DbSet<Season> Seasons {get; set;}
        public DbSet<Product> Products {get; set;}
        public DbSet<PaymentOrder> PaymentOrders {get; set;}
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfigurations());
            modelBuilder.ApplyConfiguration(new RecomendationSettingsConfigurations());
            modelBuilder.ApplyConfiguration(new ReviewConfigurations());
            modelBuilder.ApplyConfiguration(new PhotoUserConfigurations());
            modelBuilder.ApplyConfiguration(new PhotoProfileConfigurations());
            modelBuilder.ApplyConfiguration(new PhotoProfilePhotoConfigurations());
            modelBuilder.ApplyConfiguration(new UserSessionConfigurations());
            modelBuilder.ApplyConfiguration(new TheBestWeekConfigurations());
            modelBuilder.ApplyConfiguration(new UserTicketConfigurations());
            modelBuilder.ApplyConfiguration(new SeasonConfigurations());
            modelBuilder.ApplyConfiguration(new ListSeasonsConfigurations());
            modelBuilder.ApplyConfiguration(new ProductConfigurations());
            modelBuilder.ApplyConfiguration(new PaymentOrderConfigurations());
        }
    }
}