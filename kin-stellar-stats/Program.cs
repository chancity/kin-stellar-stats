using System;
using System.Collections.Generic;
using discord_web_hook_logger;
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

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(GlobalVariables.DefaultConfiguration).AddCommandLine(args).AddEnvironmentVariables();
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