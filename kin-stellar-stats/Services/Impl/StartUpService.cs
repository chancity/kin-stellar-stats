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
        //private readonly DatabaseQueueService _databaseQueueService;
        private readonly KinstatsContext _kinstatsContext;

        public StartupService(IConfigurationRoot config, IStellarService stellarService, KinstatsContext kinstatsContext)
        {
            //DatabaseQueueService databaseQueueService
            _config = config;
            _stellarService = stellarService;
            //_databaseQueueService = databaseQueueService;
            _kinstatsContext = kinstatsContext;
            _logger = DicordLogFactory.GetLogger<StartupService>(GlobalVariables.DiscordId, GlobalVariables.DiscordToken);
        }

        public async void StartAsync()
        {
            try
            {
                try
                {
                    await _kinstatsContext.Database.EnsureCreatedAsync();
                    await _kinstatsContext.Database.MigrateAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                }


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