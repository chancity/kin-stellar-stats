using System;
using System.Collections.Generic;
using discord_web_hook_logger;
using kin_stellar_stats;
using Kin.Stellar.Sdk;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kin.Horizon.Api.Poller
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Dictionary<string, string> defaultConfiguration = new Dictionary<string, string>
            {
                {"StellarService:HorizonHostname", "https://horizon-kin-ecosystem.kininfrastructure.com/"},
                {"DatabaseService:ConnectionString", "server=localhost;database=kin_test;uid=root;pwd=password"},
                {"DatabaseService:RequestPerMinute", "3000"},
                {"DiscordLogger:Id", "519614392057200670"},
                {"DiscordLogger:Token", "qggUhn6skbpcLlrU0bq2WYQfuOCORsqVE9BAhmxZsJczPgzcoTpnvG8c8jeYLvbmYljr"}
            };

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(defaultConfiguration).AddCommandLine(args).AddEnvironmentVariables();
            var configuration = builder.Build();

            GlobalVariables.DiscordId = long.Parse(configuration["DiscordLogger:Id"]);
            GlobalVariables.DiscordToken = configuration["DiscordLogger:Token"];

            var logger = DicordLogFactory.GetLogger<Program>(GlobalVariables.DiscordId, GlobalVariables.DiscordToken);

            logger.LogInformation("Entered Main Entry Point");
            Startup.RunAsync(configuration).Wait();
            logger.LogInformation("Exiting Main Entry Point");

        }
    }
}