using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Kin.Horizon.Api.Poller.Database
{
    public class DesignTimeKinstatsContextFactory : IDesignTimeDbContextFactory<KinstatsContext>
    {
        public KinstatsContext CreateDbContext(string[] args)
        {
            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(GlobalVariables.DefaultConfiguration).AddCommandLine(args).AddEnvironmentVariables();
            var configuration = configBuilder.Build();

            var builder = new DbContextOptionsBuilder<KinstatsContext>();

            var connectionString = configuration["DatabaseService:ConnectionString"];

            builder.UseMySql(connectionString);

            return new KinstatsContext(builder.Options);
        }
    }
}
