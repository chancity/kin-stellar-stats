using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using discord_web_hook_logger;
using Kin.Horizon.Api.Poller.Database;
using Kin.Horizon.Api.Poller.Database.Helpers;
using Kin.Horizon.Api.Poller.Services.Model;
using Kin.Stellar.Sdk.responses.operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kin.Horizon.Api.Poller.Services.Impl
{
    public class StatsManager
    {
        private readonly IDiscordLogger _logger;

        private readonly HttpClient _httpClient;

        public StatsManager(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _logger = DicordLogFactory.GetLogger<StatsManager>(GlobalVariables.DiscordId, GlobalVariables.DiscordToken);

            WebHookClient.RateLimitMs = 1000;
        }
        public Task HandleOperation(OperationRequest operation)
        {
            return _httpClient.PostAsync("/api/operation",
                new StringContent(JsonConvert.SerializeObject(operation), Encoding.UTF8, "application/json"));

        }

    }
}
