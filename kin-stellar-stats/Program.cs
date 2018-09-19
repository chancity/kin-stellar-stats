using System;
using kin_stellar_stats.Services.Impl;
using kin_stellar_stats;
using log4net;
using stellar_dotnet_sdk;

namespace kin_stellar_stats
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var _logger = LogManager.GetLogger(typeof(Program));


            var keyPair = KeyPair.Random();

            Console.WriteLine(keyPair.Address);
            _logger.Debug("Entered Main Entry Point");
            Startup.RunAsync(args).Wait();
            _logger.Debug("Exiting Main Entry Point");

        }
    }
}