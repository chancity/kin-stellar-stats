using System;
using System.Threading.Tasks;
using kin_stellar_stats.Database;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace kin_stellar_stats.Services.Impl
{
    public class StartupService
    {
        private readonly IConfigurationRoot _config;
        private readonly ILog _logger;
        private readonly IStellarService _stellarService;
        private readonly DatabaseQueueService _databaseQueueService;
        private readonly ManagementContext _managementContext;

        public StartupService(IConfigurationRoot config, IStellarService stellarService, ManagementContext managementContext, DatabaseQueueService databaseQueueService)
        {
            _config = config;
            _stellarService = stellarService;
            _databaseQueueService = databaseQueueService;
            _managementContext = managementContext;
            _logger = LogManager.GetLogger(typeof(StartupService));
        }

        public async void StartAsync()
        {
            try
            {
                try
                {
                    await _managementContext.Database.EnsureCreatedAsync();
                    await _managementContext.Database.MigrateAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }


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