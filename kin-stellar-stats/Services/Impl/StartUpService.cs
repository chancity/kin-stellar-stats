using System;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Configuration;

namespace kin_stellar_stats.Services.Impl
{
    public class StartupService
    {
        private readonly IConfigurationRoot _config;
        private readonly ILog _logger;
        private readonly IStellarService _stellarService;
        private readonly DatabaseQueueService _databaseQueueService;

        public StartupService(IConfigurationRoot config, IStellarService stellarService, DatabaseQueueService databaseQueueService)
        {
            _config = config;
            _stellarService = stellarService;
            _databaseQueueService = databaseQueueService;
            _logger = LogManager.GetLogger(typeof(StartupService));
        }

        public async void StartAsync()
        {
            try
            {
                _databaseQueueService.StartAsync();
                await _stellarService.StartAsync();
                _logger.Debug("Startup service has completed startup");
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message,e);
            }

        }
    }
}