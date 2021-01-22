using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Weather2DataAccessLibrary.Models;

namespace Weather2DataAccessLibrary.DataAccess
{
    class Weather2Context : DbContext
    {
        public DbSet<Sensor> Sensors { get; set; }
        public DbSet<Record> Records { get; set; }


        private string connectionString = string.Empty;

        public Weather2Context() : base()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: false);
            var configuration = builder.Build();
            connectionString = configuration.GetConnectionString("Weather2Connection");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}
