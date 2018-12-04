using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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

        public StellarService(IConfigurationRoot config, ManagementContext managementContext, DatabaseQueueService databaseQueueService)
        {
            _config = config;
            _managementContext = managementContext;
            //_databaseQueueService = databaseQueueService;
            _statsManager = new StatsManager();
            _logger = DicordLogFactory.GetLogger<StellarService>(GlobalVariables.DiscordId,
                GlobalVariables.DiscordToken);

            ServicePointManager.DefaultConnectionLimit = 300;
            _logger.LogInformation($"Stellar service is using endpoint {_config["StellarService:HorizonHostname"]}");
            _server = new Server(_config["StellarService:HorizonHostname"]);
            Network.UsePublicNetwork();
        }
        private DateTime? _sendQueueInfoMessageTime;
        private long _totalRequest = 0;
        private Stopwatch _startTime = new Stopwatch();

        public async Task StartAsync()
        {
            _startTime.Start();
            long pagingTokenLong = await GetCurrentCursorFromDatabase("FlattenedOperation");
            string pagingToken = pagingTokenLong.ToString();

            _logger.LogDebug($"Starting page token is {pagingToken}");

            await DeleteLastCursorId(pagingTokenLong);

            List<Task> task = new List<Task>();
            _operationsRequestBuilder = _server.Operations.Cursor(pagingToken).Limit(200);
            _eventSource = _operationsRequestBuilder.Stream(async (sender, response) =>
            {
                var total = Interlocked.Increment(ref _totalRequest);
                Task[] arrayCopy = null;

                lock (task)
                {
                    if (task.Count == 200)
                    {
                        arrayCopy = task.ToArray();
                        task.Clear();
                    }
                    else
                    {
                        task.Add(HandleResponse(response));
                    }
                }

                if(arrayCopy != null)
                    await Task.WhenAll(arrayCopy).ConfigureAwait(false);

                if (_sendQueueInfoMessageTime == null || DateTime.Now >= _sendQueueInfoMessageTime)
                {
                    _sendQueueInfoMessageTime = DateTime.Now.AddMinutes(1);

                   

                    _logger.LogInformation($"Total operations parsed {_totalRequest}");
                    _logger.LogInformation($"Currently queued operations {task.Count}");
                    _logger.LogInformation($"Current paging token '{response.PagingToken}");
                    var rpm = _startTime.Elapsed.Minutes > 0 ? $"{_totalRequest / _startTime.Elapsed.Minutes} request handled per minute ({_startTime.Elapsed.Minutes}m)" : "";
                    if(!string.IsNullOrEmpty(rpm))
                        _logger.LogInformation($"{rpm}");

                    _statsManager.OutPutToDiscord();
                }
            });

            _eventSource.Error += (sender, args) => { _logger.LogError(args.Exception.Message); };
            _eventSource.Connect().Wait();

        }

        private async Task HandleResponse(OperationResponse response)
        {
            try
            {
                var operation = response;
                var transactions = await _server.Transactions.Transaction(response.TransactionHash).ConfigureAwait(false);
                var effect = await _server.Effects.ForOperation(response.Id).Order(OrderDirection.ASC).Limit(1).Execute().ConfigureAwait(false);


                var flattenOperation = FlattenOperationFactory.GibeFlattenedOperation(operation, transactions, effect.Records.FirstOrDefault());
               // var kinAccounts = new HashSet<KinAccount>();


              // if (response is PaymentOperationResponse paymentOperation)
              // {
              //     var accountTo = await _server.Accounts.Account(paymentOperation.To).ConfigureAwait(false);
              //     kinAccounts.Add(new KinAccount(accountTo));
              //
              //     var accountFrom = await _server.Accounts.Account(paymentOperation.From).ConfigureAwait(false);
              //     kinAccounts.Add(new KinAccount(accountFrom));
              // }
              // else if (response is CreateAccountOperationResponse createAccountOperation)
              // {
              //     var accountCreated = await _server.Accounts.Account(createAccountOperation.Account).ConfigureAwait(false);
              //     var account = new KinAccount(accountCreated);
              //     kinAccounts.Add(account);
              // }

               // var toQueue = new DatabaseQueueModel(flattenOperation, kinAccounts.ToArray());
               // _databaseQueueService.EnqueueCommand(toQueue);

                _statsManager.HandleOperation(flattenOperation);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e.Message);
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