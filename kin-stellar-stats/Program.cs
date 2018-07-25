using kin_stellar_stats.Services.Impl;
using kin_stellar_stats;
using log4net;

namespace kin_stellar_stats
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var _logger = LogManager.GetLogger(typeof(Program));
            _logger.Debug("Entered Main Entry Point");
            Startup.RunAsync(args).Wait();
            _logger.Debug("Exiting Main Entry Point");

        }
    }
}