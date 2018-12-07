using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using discord_web_hook_logger;
using Kin.Horizon.Api.Poller.Database;
using Kin.Horizon.Api.Poller.Services.Model;
using Kin.Stellar.Sdk;
using Kin.Stellar.Sdk.requests;
using Kin.Stellar.Sdk.responses.operations;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kin.Horizon.Api.Poller.Services.Impl
{
    public class StellarService : IStellarService
    {
        private readonly IConfigurationRoot _config;
        private readonly KinstatsContext _kinstatsContext;
        //private readonly DatabaseQueueService _databaseQueueService;
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
        private int _maxQueue = 1000;
        private readonly AutoResetEvent _queueNotifier1;
        private readonly System.Timers.Timer _timer;

        public StellarService(IConfigurationRoot config, KinstatsContext kinstatsContext)
        {
            _config = config;
            _queueNotifier1 = new AutoResetEvent(false);

            _kinstatsContext = kinstatsContext;
            _statsManager = new StatsManager(kinstatsContext);
            _logger = DicordLogFactory.GetLogger<StellarService>(GlobalVariables.DiscordId,GlobalVariables.DiscordToken);
            _operationsToHandleQueue = new ConcurrentQueue<OperationResponse>();
            ServicePointManager.DefaultConnectionLimit = 300;
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
            ulong pagingTokenLong = await GetCurrentCursorFromDatabase("operation");
            string pagingToken = pagingTokenLong.ToString();

            _logger.LogDebug($"Starting page token is {pagingToken}");

           // List<Task> task = new List<Task>();
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

                    if (_totalRequest != 0 && _totalRequest % 2000 == 0)
                    {
                        await _statsManager.SaveData();
                    }

                    if (!_operationsToHandleQueue.TryDequeue(out var operation)) continue;

                    var success = await _statsManager.PopulateSavedActiveWallets(operation);

                    if (success)
                    {
                        HandleResponse(operation);
                    }
                    else
                    {
                        _operationsToHandleQueue.Enqueue(operation);
                    }
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
                var flattenOperation = FlattenOperationFactory.GibeFlattenedOperation(operation, transactions);
                _statsManager.HandleOperation(flattenOperation);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e.Message);
                _operationsToHandleQueue.Enqueue(operation);
            }
            finally
            {
                Interlocked.Decrement(ref _queueCounter);
            }
        }

        private async Task<ulong> GetCurrentCursorFromDatabase(string cursorType)
        {
            var pagination = await _kinstatsContext.Pagination.AsNoTracking().SingleOrDefaultAsync(x => x.CursorType == cursorType);
            return pagination?.CursorId ?? 0;
        }
    }
}
