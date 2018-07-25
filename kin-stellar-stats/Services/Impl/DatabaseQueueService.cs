using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using kin_stellar_stats.Database;
using kin_stellar_stats.Database.Models;
using kin_stellar_stats.Database.StellarObjectWrappers;
using kin_stellar_stats.Services.Model;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace kin_stellar_stats.Services.Impl
{
    public class DatabaseQueueService
    {
        private readonly ConcurrentQueue<DatabaseQueueModel> _databaseCommandQueue;
        private readonly ILog _logger;
        private readonly IConfigurationRoot _config;
        private readonly ManagementContext _managementContext;
        private readonly AutoResetEvent _queueNotifier = new AutoResetEvent(false);
        private readonly AutoResetEvent _queueNotifier1 = new AutoResetEvent(false);
        private readonly System.Timers.Timer _timer = new System.Timers.Timer(50);
        private int _queueCounter;
        private int _maxQueue = 1;

        public DatabaseQueueService(IConfigurationRoot config, ManagementContext managementContext)
        {
            _config = config;
            _managementContext = managementContext;
            _queueCounter = 0;
            _logger = LogManager.GetLogger(typeof(StellarService));
            _databaseCommandQueue = new ConcurrentQueue<DatabaseQueueModel>();
        }

        public void EnqueueCommand(DatabaseQueueModel command)
        {
            _databaseCommandQueue.Enqueue(command);

            _queueNotifier.Set();
        }

        public void StartAsync()
        {
            _timer.Elapsed += Timer_tick;
            _timer.Enabled = true;
            _timer.Start();
            Start();
            _logger.Debug("Database CommandQueueLoop service has started");
        }

        private void Timer_tick(object sender, ElapsedEventArgs e)
        {
            if (_queueCounter < _maxQueue)
                _queueNotifier1.Set();
        }

        private void Start()
        {
            Task.Factory.StartNew(CommandQueueLoop, TaskCreationOptions.LongRunning);
        }

        private void CommandQueueLoop()
        {
            while (true)
            {
                _queueNotifier.WaitOne();
                while (!_databaseCommandQueue.IsEmpty)
                {
                    if (_queueCounter >= _maxQueue) _queueNotifier1.WaitOne();
                    if (!_databaseCommandQueue.TryDequeue(out DatabaseQueueModel command)) continue;
                    HandleDatabaseQueueModel(command);

                }
            }
        }
        public async void HandleDatabaseQueueModel(DatabaseQueueModel databaseCommand)
        {
            Interlocked.Increment(ref _queueCounter);
            ManagementContext context = null;
            try
            {
                var contextOptions = new DbContextOptionsBuilder<ManagementContext>();
                contextOptions.UseMySql(_config["DatabaseService:ConnectionString"]);
                contextOptions.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                
                context = new ManagementContext(contextOptions.Options);
                
                foreach (KinAccount account in databaseCommand.KinAccounts)
                {

                        var kinAccount = await context.KinAccounts.Include(c => c.FlattenedBalance).SingleOrDefaultAsync(c => c.Id == account.Id);

                        if (kinAccount != null)
                        {
                            if (databaseCommand.Operation is FlattenCreateAccountOperation)
                            {
                                kinAccount.CreatedAt = databaseCommand.Operation.CreatedAt;
                                kinAccount.Memo = databaseCommand.Operation.Memo;
                            }
                            
                            context.FlattenedBalance.RemoveRange(kinAccount.FlattenedBalance);

                            kinAccount.FlattenedBalance = account.FlattenedBalance;
                            kinAccount.LastActive = databaseCommand.Operation.CreatedAt;
                            context.KinAccounts.Update(kinAccount);

                          //foreach (FlattenedBalance flattenedBalance in kinAccount.FlattenedBalance)
                          //{
                          //    var balance = await context.FlattenedBalance.SingleOrDefaultAsync(c => c.KinAccountId == flattenedBalance.KinAccountId && c.AssetType == flattenedBalance.AssetType);
                          //
                          //    if (balance != null)
                          //    {
                          //        balance.BalanceString = flattenedBalance.BalanceString;
                          //        balance.Limit = flattenedBalance.Limit;
                          //    }
                          //}
                        }
                        else
                        {
                            if (databaseCommand.Operation is FlattenCreateAccountOperation)
                            {
                                account.CreatedAt = databaseCommand.Operation.CreatedAt;
                                account.Memo = databaseCommand.Operation.Memo;
                        }

                            account.LastActive = databaseCommand.Operation.CreatedAt;
                            await context.KinAccounts.AddAsync(account);
                        }
                    }


                     var op = await context.FlattenedOperation.SingleOrDefaultAsync(c => c.Id == databaseCommand.Operation.Id);

                     if (op != null)
                     {
                         context.FlattenedOperation.Update(op);
                     }
                     else
                     {
                         await context.FlattenedOperation.AddAsync(databaseCommand.Operation);
                     }

                await context.SaveChangesAsync();

                await context.Database.ExecuteSqlCommandAsync(
                    $"INSERT INTO paginations SET CursorType = 'flattenedoperation'," +
                    $" PagingToken = '{databaseCommand.Operation.PagingToken}'" +
                    $" ON DUPLICATE KEY UPDATE PagingToken = IF({databaseCommand.Operation.PagingToken} > PagingToken, '{databaseCommand.Operation.PagingToken}', PagingToken)");

            }
            catch (Exception e)
            {
                context?.Dispose();
                EnqueueCommand(databaseCommand);
                _logger.Error(e.Message, e);
            }
            finally
            {
                Interlocked.Decrement(ref _queueCounter);
            }
           
        }
    }
}
