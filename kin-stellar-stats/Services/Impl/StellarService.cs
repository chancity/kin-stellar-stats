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
using Kin.Horizon.Api.Poller.Database.StellarObjectWrappers;
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
        private readonly ManagementContext _managementContext;
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
        private readonly AutoResetEvent QueueNotifier;
        private readonly AutoResetEvent QueueNotifier1;
        private readonly System.Timers.Timer Timer;

        public StellarService(IConfigurationRoot config, ManagementContext managementContext, DatabaseQueueService databaseQueueService)
        {
            _config = config;
            QueueNotifier = new AutoResetEvent(false);
            QueueNotifier1 = new AutoResetEvent(false);
           
            _managementContext = managementContext;
            //_databaseQueueService = databaseQueueService;
            _statsManager = new StatsManager();
            _logger = DicordLogFactory.GetLogger<StellarService>(GlobalVariables.DiscordId,
                GlobalVariables.DiscordToken);
            _operationsToHandleQueue = new ConcurrentQueue<OperationResponse>();
            ServicePointManager.DefaultConnectionLimit = 300;
            _logger.LogInformation($"Stellar service is using endpoint {_config["StellarService:HorizonHostname"]}");
            _server = new Server(_config["StellarService:HorizonHostname"]);
            Network.UsePublicNetwork();

            _startTime = new Stopwatch();
            var queueThread = new Task(HandleResponseQueue, TaskCreationOptions.LongRunning);
            queueThread.Start();

            Timer = new System.Timers.Timer(50);
            Timer.Elapsed += Timer_tick;
            Timer.Enabled = true;
            Timer.Start();
        }




        public async Task StartAsync()
        {
            _startTime.Start();
            long pagingTokenLong = await GetCurrentCursorFromDatabase("FlattenedOperation");
            string pagingToken = pagingTokenLong.ToString();

            _logger.LogDebug($"Starting page token is {pagingToken}");

            await DeleteLastCursorId(pagingTokenLong);

           // List<Task> task = new List<Task>();
            _operationsRequestBuilder = _server.Operations.Cursor(pagingToken).Limit(200);
            _eventSource = _operationsRequestBuilder.Stream((sender, response) =>
            {
                _operationsToHandleQueue.Enqueue(response);
                QueueNotifier.Set();

                if (_sendQueueInfoMessageTime == null || DateTime.Now >= _sendQueueInfoMessageTime)
                {
                    _sendQueueInfoMessageTime = DateTime.Now.AddMinutes(1);

                    _logger.LogInformation($"Total operations parsed {_totalRequest}");
                    _logger.LogInformation($"Currently queued operations {_operationsToHandleQueue.Count}");
                    _logger.LogInformation($"Current paging token '{response.PagingToken}");
                    var rpm = _startTime.Elapsed.Minutes > 0 ? $"{_totalRequest / _startTime.Elapsed.Minutes} request handled per minute ({_startTime.Elapsed.Minutes}m)" : "";

                    if (!string.IsNullOrEmpty(rpm))
                        _logger.LogInformation($"{rpm}");

                    _statsManager.OutPutToDiscord();
                }
            });

            _eventSource.Error += (sender, args) => { _logger.LogError(args.Exception.Message); };
            _eventSource.Connect().Wait();

        }
        private void Timer_tick(object sender, ElapsedEventArgs e)
        {
            if (_queueCounter < _maxQueue)
                QueueNotifier1.Set();

        }

        private void HandleResponseQueue()
        {
            while (true)
            {
                QueueNotifier.WaitOne();
                while (!_operationsToHandleQueue.IsEmpty)
                {

                    if (_queueCounter >= _maxQueue) QueueNotifier1.WaitOne();
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
                var transactions =
                    await _server.Transactions.Transaction(operation.TransactionHash).ConfigureAwait(false);
                var flattenOperation = FlattenOperationFactory.GibeFlattenedOperation(operation, transactions);
                _statsManager.HandleOperation(flattenOperation);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e.Message);
            }
            finally
            {
                Interlocked.Decrement(ref _queueCounter);
            }
        }
        private async Task DeleteLastCursorId(long pagingToken, params string[] operationTypes)
        {
            if (operationTypes.Length == 0)
            {
                operationTypes = new[] { "FlattenedOperation" };
            }

            List<Task<int>> tasks = operationTypes
                .Select(async operationType => 
                    await _managementContext
                        .Database
                        .ExecuteSqlCommandAsync(string.Format("DELETE FROM {0} WHERE Id = {1};", 
                        operationType, 
                        pagingToken))).ToList();

            await Task.WhenAll(tasks);
        }

        private async Task<long> GetCurrentCursorFromDatabase(string cursorType)
        {
            var pagination = await _managementContext.Paginations.AsNoTracking().SingleOrDefaultAsync(x => x.CursorType == cursorType);
            return pagination?.PagingToken ?? 0;
        }
    }
}