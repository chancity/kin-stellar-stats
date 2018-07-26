using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace kin_stellar_stats.Database
{
    public class DesignTimeManagementContextFactory : IDesignTimeDbContextFactory<ManagementContext>
    {
        public ManagementContext CreateDbContext(string[] args)
        {
            Dictionary<string, string> defaultConfiguration = new Dictionary<string, string>
            {
                {"StellarService:HorizonHostname", "https://horizon-kin-ecosystem.kininfrastructure.com/"},
                {"DatabaseService:ConnectionString", "server=localhost;database=kin_test;uid=root;pwd=giveME@ccess"},
                {"DatabaseService:RequestPerMinute", "3000"}
            };

            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(defaultConfiguration).AddCommandLine(args);
            var configuration = configBuilder.Build();

            var builder = new DbContextOptionsBuilder<ManagementContext>();

            var connectionString = configuration["DatabaseService:ConnectionString"];

            builder.UseMySql(connectionString);

            return new ManagementContext(builder.Options);
        }
    }
}
