using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using discord_web_hook_logger;
using Kin.Horizon.Api.Poller.Database;
using Kin.Horizon.Api.Poller.Services;
using Kin.Horizon.Api.Poller.Services.Impl;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kin.Horizon.Api.Poller
{
    public static class GlobalVariables
    {
        public static long DiscordId;
        public static string DiscordToken;

        public static Dictionary<string, string> DefaultConfiguration = new Dictionary<string, string>
        {
            {"StellarService:HorizonHostname", "https://horizon-block-explorer.kininfrastructure.com"},
            {"DiscordLogger:Id", "519614392057200670"},
            {"DiscordLogger:Token", "qggUhn6skbpcLlrU0bq2WYQfuOCORsqVE9BAhmxZsJczPgzcoTpnvG8c8jeYLvbmYljr"},
            { "KinStats_Api", "http://127.0.0.1:5000"},
            { "KinStats_Api_Key", "SuperSecretYo"}

        };
    }
    public class Startup
    {
        private readonly IConfigurationRoot _configuration;
        private static readonly IDiscordLogger Logger;

        static Startup()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("Kin.Horizon.Api.Poller.dll.config"));

            Logger = DicordLogFactory.GetLogger<Startup>(GlobalVariables.DiscordId, GlobalVariables.DiscordToken);

        }

        private Startup(IConfigurationRoot config)
        {
            _configuration = config;
        }

        public static async Task RunAsync(IConfigurationRoot config)
        {
            try
            {
                Startup startup = new Startup(config);
                await startup.RunAsync();
            }
            catch (Exception e)
            {
                Logger.LogCritical(e.Message, e);
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
            var apiKey = _configuration["KinStats_Api_Key"];
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_configuration["KinStats_Api"]);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            services.AddSingleton(_configuration)
                .AddSingleton(httpClient)
                .AddSingleton<StatsManager>()
                .AddSingleton<IStellarService, StellarService>()
                .AddSingleton<StartupService>();
        }
    }
}