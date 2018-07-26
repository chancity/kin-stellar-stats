using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using kin_stellar_stats.Database;
using kin_stellar_stats.Database.Models;
using kin_stellar_stats.Database.StellarObjectWrappers;
using kin_stellar_stats.Services.Model;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using stellar_dotnet_sdk;
using stellar_dotnet_sdk.requests;
using stellar_dotnet_sdk.responses.operations;

namespace kin_stellar_stats.Services.Impl
{
    public class StellarService : IStellarService
    {
        private readonly IConfigurationRoot _config;
        private readonly ManagementContext _managementContext;
        private readonly DatabaseQueueService _databaseQueueService;
        private readonly ILog _logger;
        private readonly Server _server;
        private EventSource _eventSource;
        private OperationsRequestBuilder _operationsRequestBuilder;

        public StellarService(IConfigurationRoot config, ManagementContext managementContext, DatabaseQueueService databaseQueueService)
        {
            _config = config;
            _managementContext = managementContext;
            _databaseQueueService = databaseQueueService;
            _logger = LogManager.GetLogger(typeof(StellarService));
            _logger.Debug($"Setting stellar server too {_config["StellarService:HorizonHostname"]}");
            _server = new Server(_config["StellarService:HorizonHostname"]);
            Network.UsePublicNetwork();
        }

        public async Task StartAsync()
        {
            long pagingTokenLong = await GetCurrentCursorFromDatabase("FlattenedOperation");
            string pagingToken = pagingTokenLong.ToString();

            _logger.Debug($"{nameof(pagingToken)}: {pagingToken}");

            await DeleteLastCursorId(pagingTokenLong);

            _operationsRequestBuilder = _server.Operations.Cursor(pagingToken).Limit(20);
            _eventSource = _operationsRequestBuilder.Stream(async (sender, response) =>
            {
                //_logger.Debug($"Got operation response {response.Id}");
                try
                {
                    var operation = response;
                    var transactions = await _server.Transactions.Transaction(response.TransactionHash);
                    var effect = await _server.Effects.ForOperation(response.Id).Order(OrderDirection.ASC).Limit(1).Execute();


                    var flattenOperation = FlattenOperationFactory.GibeFlattenedOperation(operation, transactions, effect.Records.FirstOrDefault());
                    var kinAccounts = new HashSet<KinAccount>();


                    if (response is PaymentOperationResponse paymentOperation)
                    {
                        var accountTo = await _server.Accounts.Account(paymentOperation.To);
                        kinAccounts.Add(new KinAccount(accountTo));

                        var accountFrom = await _server.Accounts.Account(paymentOperation.From);
                        kinAccounts.Add(new KinAccount(accountFrom));
                    }
                    else if (response is CreateAccountOperationResponse createAccountOperation)
                    {
                        var accountCreated = await _server.Accounts.Account(createAccountOperation.Account);
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

            });

            _eventSource.Error += (sender, args) => { _logger.Debug(args.Exception.Message, args.Exception); };
            _eventSource.Connect();

        }

        private async Task DeleteLastCursorId(long pagingToken, params string[] operationTypes)
        {
            if (operationTypes.Length == 0)
            {
                operationTypes = new[] { "FlattenedOperation" };
            }

            List<Task<int>> tasks = operationTypes
                .Select(async operationType =>
                    await _managementContext.Database.ExecuteSqlCommandAsync(string.Format("DELETE FROM {0} WHERE Id = {1};", operationType, pagingToken)))
                .ToList();

            await Task.WhenAll(tasks);
        }

        private async Task<long> GetCurrentCursorFromDatabase(string cursorType)
        {
            var pagination = await _managementContext.Paginations.AsNoTracking().SingleOrDefaultAsync(x => x.CursorType == cursorType);
            return pagination?.PagingToken ?? 0;
        }
    }
}