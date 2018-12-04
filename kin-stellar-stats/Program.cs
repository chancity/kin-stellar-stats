using System;
using kin_stellar_stats;
using Kin.Stellar.Sdk;
using log4net;

namespace Kin.Horizon.Api.Poller
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