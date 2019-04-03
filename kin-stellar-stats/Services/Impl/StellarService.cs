using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using discord_web_hook_logger;
using Kin.Horizon.Api.Poller.Database;
using Kin.Horizon.Api.Poller.Services.Model;
using Kin.Horizon.Api.Poller.Services.Model.ApiResponse;
using Kin.Stellar.Sdk;
using Kin.Stellar.Sdk.requests;
using Kin.Stellar.Sdk.responses.operations;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kin.Horizon.Api.Poller.Services.Impl
{
    public class StellarService : IStellarService
    {
        private readonly IConfigurationRoot _config;

        private readonly IDiscordLogger _logger;
        private readonly Server _server;
        private IEventSource _eventSource;
        private OperationsRequestBuilder _operationsRequestBuilder;
        private readonly StatsManager _statsManager;
        private readonly ConcurrentQueue<OperationResponse> _operationsToHandleQueue;
        private DateTime? _sendQueueInfoMessageTime;
        private long _totalRequest;
        private readonly Stopwatch _startTime;
        private int _queueCounter;
        private int _maxQueue = 400;
        private readonly AutoResetEvent _queueNotifier1;
        private readonly System.Timers.Timer _timer;
        private readonly HttpClient _httpClient;
        public StellarService(IConfigurationRoot config, HttpClient httpClient, StatsManager statsManager)
        {
            _statsManager = statsManager;
            _config = config;
            _queueNotifier1 = new AutoResetEvent(false);
            _httpClient = httpClient;

            _logger = DicordLogFactory.GetLogger<StellarService>(GlobalVariables.DiscordId,GlobalVariables.DiscordToken);
            _operationsToHandleQueue = new ConcurrentQueue<OperationResponse>();
            ServicePointManager.DefaultConnectionLimit = 100;
            _logger.LogInformation($"Stellar service is using endpoint {_config["StellarService:HorizonHostname"]}");
            _server = new Server(_config["StellarService:HorizonHostname"]);
            Network.UsePublicNetwork();

            _startTime = new Stopwatch();
            var queueThread = new Task(HandleResponseQueue, TaskCreationOptions.LongRunning);
            queueThread.Start();

            _timer = new System.Timers.Timer(50);
            _timer.Elapsed += Timer_tick;
            _timer.Enabled = true;
            _timer.Start();
        }




        public async Task StartAsync()
        {
            _startTime.Start();
            long pagingTokenLong = await GetCurrentCursorFromDatabase("operation");
            string pagingToken = pagingTokenLong.ToString();


            _logger.LogDebug($"Starting page token is {pagingToken}");

            _operationsRequestBuilder = _server.Operations.Cursor(pagingToken).Limit(200);
            _eventSource = _operationsRequestBuilder.Stream((sender, response) =>
            {
                _operationsToHandleQueue.Enqueue(response);
               
                if (_sendQueueInfoMessageTime == null || DateTime.Now >= _sendQueueInfoMessageTime)
                {
                    _sendQueueInfoMessageTime = DateTime.Now.AddMinutes(1);

                    _logger.LogInformation($"Total operations parsed {_totalRequest}");
                    _logger.LogInformation($"Currently queued operations {_operationsToHandleQueue.Count}");
                    _logger.LogInformation($"Current paging token '{response.PagingToken}");
                    var rpm = _startTime.Elapsed.Minutes > 0 ? $"{_totalRequest / (int)_startTime.Elapsed.TotalMinutes} request handled per minute ({(int)_startTime.Elapsed.TotalMinutes}m)" : "";

                    if (!string.IsNullOrEmpty(rpm))
                        _logger.LogInformation($"{rpm}");
                }
            });

            _eventSource.Error += (sender, args) => { _logger.LogError(args.Exception.Message); };
            _eventSource.Connect().Wait();

        }
        private void Timer_tick(object sender, ElapsedEventArgs e)
        {
            if (_queueCounter < _maxQueue)
                _queueNotifier1.Set();

        }

        private async void HandleResponseQueue()
        {
            while (true)
            {
                await Task.Delay(1);
                while (!_operationsToHandleQueue.IsEmpty)
                {

                    if (_queueCounter >= _maxQueue) _queueNotifier1.WaitOne();
                    if (!_operationsToHandleQueue.TryDequeue(out var operation)) continue;
                    HandleResponse(operation);
  
                }
            }
        }

        private async Task HandleResponse(OperationResponse operation)
        {
            Interlocked.Increment(ref _totalRequest);
            Interlocked.Increment(ref _queueCounter);
            try
            {
                var transactions = await _server.Transactions.Transaction(operation.TransactionHash).ConfigureAwait(false);
                var operationRequest = OperationRequestFactory.GibeFlattenedOperation(operation, transactions);

   
                await _statsManager.HandleOperation(operationRequest);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e.Message);

                if(!e.Message.Contains("Not Found"))
                    _operationsToHandleQueue.Enqueue(operation);
            }
            finally
            {
                Interlocked.Decrement(ref _queueCounter);
            }
        }

        private async Task<long> GetCurrentCursorFromDatabase(string cursorType)
        {
            try
            {
                
                var ret = await _httpClient.GetStringAsync("/api/pagingtoken/operation").ConfigureAwait(false);
                var pt = JsonConvert.DeserializeObject<BaseResponseData<PagingToken>>(ret);

                return pt.Data.Value;

            }
            catch (HttpRequestException hex)
            {
                Console.WriteLine(hex);
                if (hex.Message.Contains("Response status code does not indicate success: 404 (Not Found)."))
                {
                    return 0;
                }

                throw;
            }
            catch (WebException wex)
            {

                Console.WriteLine(wex);
                wex?.Response?.Dispose();
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
