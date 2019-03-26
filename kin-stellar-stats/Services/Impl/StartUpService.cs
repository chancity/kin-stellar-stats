using System;
using discord_web_hook_logger;
using Kin.Horizon.Api.Poller.Database;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kin.Horizon.Api.Poller.Services.Impl
{
    public class StartupService
    {
        private readonly IConfigurationRoot _config;
        private readonly IDiscordLogger _logger;
        private readonly IStellarService _stellarService;

        public StartupService(IConfigurationRoot config, IStellarService stellarService)
        {
            //DatabaseQueueService databaseQueueService
            _config = config;
            _stellarService = stellarService;

            _logger = DicordLogFactory.GetLogger<StartupService>(GlobalVariables.DiscordId, GlobalVariables.DiscordToken);
        }

        public async void StartAsync()
        {
            try
            {
               // _databaseQueueService.StartAsync();
                await _stellarService.StartAsync();
                _logger.LogInformation("Startup service has completed");
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message,e);
            }

        }
    }
}