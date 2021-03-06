﻿using Microsoft.EntityFrameworkCore;
using MyTradingApp.Domain;

namespace MyTradingApp.Core.Persistence
{
    public class ApplicationContext : DbContext, IApplicationContext
    {
        public ApplicationContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Trade> Trades
        {
            get; set;
        }

        public DbSet<StopLoss> Stops
        {
            get; set;
        }

        public DbSet<Setting> Settings
        {
            get; set;
        }

        public DbSet<Exit> Exits
        {
            get; set;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.RemovePluralizingTableNameConvention();
            base.OnModelCreating(modelBuilder);
        }
    }
}