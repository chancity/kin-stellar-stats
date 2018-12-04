using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using kin_stellar_stats.Services;
using Kin.Horizon.Api.Poller.Database;
using Kin.Horizon.Api.Poller.Database.StellarObjectWrappers;
using Kin.Horizon.Api.Poller.Services.Model;
using Kin.Stellar.Sdk;
using Kin.Stellar.Sdk.requests;
using Kin.Stellar.Sdk.responses.operations;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Kin.Horizon.Api.Poller.Services.Impl
{
    public class StellarService : IStellarService
    {
        private readonly IConfigurationRoot _config;
        private readonly ManagementContext _managementContext;
        private readonly DatabaseQueueService _databaseQueueService;
        private readonly ILog _logger;
        private readonly Server _server;
        private IEventSource _eventSource;
        private OperationsRequestBuilder _operationsRequestBuilder;

        public StellarService(IConfigurationRoot config, ManagementContext managementContext, DatabaseQueueService databaseQueueService)
        {
            _config = config;
            _managementContext = managementContext;
            _databaseQueueService = databaseQueueService;
            _logger = LogManager.GetLogger(typeof(StellarService));
            ServicePointManager.DefaultConnectionLimit = 300;
            _logger.Debug($"Setting stellar server too {_config["StellarService:HorizonHostname"]}");
            _server = new Server(_config["StellarService:HorizonHostname"]);
            Network.UsePublicNetwork();
        }

        public int totalRequest = 0;
        public Stopwatch startTime = new Stopwatch();
        public async Task StartAsync()
        {
            startTime.Start();
            long pagingTokenLong = await GetCurrentCursorFromDatabase("FlattenedOperation");
            string pagingToken = pagingTokenLong.ToString();

            _logger.Debug($"{nameof(pagingToken)}: {pagingToken}");

            await DeleteLastCursorId(pagingTokenLong);

            List<Task> task = new List<Task>();
            _operationsRequestBuilder = _server.Operations.Cursor(pagingToken).Limit(200);
            _eventSource = _operationsRequestBuilder.Stream(async (sender, response) =>
            {
                var total = Interlocked.Increment(ref totalRequest);
                Task[] arrayCopy = null;

                lock (task)
                {
                    var rpm = startTime.Elapsed.Minutes > 0 ? $" | request handled per minute( {startTime.Elapsed.Minutes}m )  {totalRequest / startTime.Elapsed.Minutes} ": "";
                    Console.WriteLine($"{task.Count} operations queued | '{response.PagingToken}'{rpm}");
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
            });

            _eventSource.Error += (sender, args) => { _logger.Debug(args.Exception.Message, args.Exception); };
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
                var kinAccounts = new HashSet<KinAccount>();


                if (response is PaymentOperationResponse paymentOperation)
                {
                    var accountTo = await _server.Accounts.Account(paymentOperation.To).ConfigureAwait(false);
                    kinAccounts.Add(new KinAccount(accountTo));

                    var accountFrom = await _server.Accounts.Account(paymentOperation.From).ConfigureAwait(false);
                    kinAccounts.Add(new KinAccount(accountFrom));
                }
                else if (response is CreateAccountOperationResponse createAccountOperation)
                {
                    var accountCreated = await _server.Accounts.Account(createAccountOperation.Account).ConfigureAwait(false);
                    var account = new KinAccount(accountCreated);
                    kinAccounts.Add(account);
                }

                var toQueue = new DatabaseQueueModel(flattenOperation, kinAccounts.ToArray());
                _databaseQueueService.EnqueueCommand(toQueue);


            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
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