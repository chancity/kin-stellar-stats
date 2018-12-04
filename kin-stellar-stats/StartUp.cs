using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using kin_stellar_stats.Database;
using kin_stellar_stats.Services;
using Kin.Horizon.Api.Poller.Database;
using Kin.Horizon.Api.Poller.Services.Impl;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace kin_stellar_stats
{
    public class Startup
    {
        private static readonly ILog Logger;
        private readonly IConfigurationRoot _configuration;

        static Startup()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("kin_stellar_stats.dll.config"));
            Logger = LogManager.GetLogger(typeof(Startup));
        }

        private Startup(string[] args)
        {
            Dictionary<string, string> defaultConfiguration = new Dictionary<string, string>
            {
                {"StellarService:HorizonHostname", "https://horizon-kin-ecosystem.kininfrastructure.com/"},
                {"DatabaseService:ConnectionString", "server=localhost;database=kin_test;uid=root;pwd=password"},
                {"DatabaseService:RequestPerMinute", "3000"}
            };

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(defaultConfiguration).AddCommandLine(args);

            _configuration = builder.Build();
        }

        public static async Task RunAsync(string[] args)
        {
            try
            {
                Startup startup = new Startup(args);
                await startup.RunAsync();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message, e);
                Console.ReadLine();

                throw;
            }
        }

        private async Task RunAsync()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider provider = services.BuildServiceProvider();

            provider.GetRequiredService<StartupService>().StartAsync();
            await Task.Delay(-1);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton(_configuration)
                .AddDbContext<ManagementContext>(options =>
                {
                    options.UseMySql(_configuration["DatabaseService:ConnectionString"]);
                    options.EnableSensitiveDataLogging();
                })
                .AddSingleton<DatabaseQueueService>()
                .AddSingleton<IStellarService, StellarService>()
                .AddSingleton<StartupService>();
        }
    }
}