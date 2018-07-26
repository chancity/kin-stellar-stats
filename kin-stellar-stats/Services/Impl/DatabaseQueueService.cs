using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Chaos.NaCl;
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

        private async void CommandQueueLoop()
        {
            while (true)
            {
                _queueNotifier.WaitOne();
                while (!_databaseCommandQueue.IsEmpty)
                {
                    if (_queueCounter >= _maxQueue) _queueNotifier1.WaitOne();
                    if (!_databaseCommandQueue.TryDequeue(out DatabaseQueueModel command)) continue;
                    await HandleDatabaseQueueModel(command);

                }
            }
        }
        public async Task HandleDatabaseQueueModel(DatabaseQueueModel databaseCommand)
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
                        var kinAccount = await context.KinAccounts.SingleOrDefaultAsync(c => c.Id == account.Id);

                        if (kinAccount != null)
                        {
                            if (databaseCommand.Operation is FlattenCreateAccountOperation)
                            {
                                kinAccount.CreatedAt = databaseCommand.Operation.CreatedAt;
                                kinAccount.Memo = databaseCommand.Operation.Memo;
                            }

                            if (databaseCommand.Operation is FlattenPaymentOperation aa)
                            {
                                if (kinAccount.Id.Equals(aa.To))
                                {
                                    kinAccount.AccountCreditedCount += 1;
                                    kinAccount.AccountCreditedVolume += aa.Amount;
                                }
                                else if (kinAccount.Id.Equals(aa.From))
                                {
                                    kinAccount.AccountDebitedCount += 1;
                                    kinAccount.AccountDebitedVolume += aa.Amount;
                                }
                                    
                            }

                            kinAccount.Balance = account.Balance;
                            kinAccount.LastActive = databaseCommand.Operation.CreatedAt;


                            context.KinAccounts.Update(kinAccount);
                        }
                        else
                        {
                            if (databaseCommand.Operation is FlattenCreateAccountOperation)
                            {
                                account.CreatedAt = databaseCommand.Operation.CreatedAt;
                                account.Memo = databaseCommand.Operation.Memo;
                            }

                            if (databaseCommand.Operation is FlattenPaymentOperation aa)
                            {
                                if (account.Id.Equals(aa.To))
                                {
                                    account.AccountCreditedCount += 1;
                                    account.AccountCreditedVolume += aa.Amount;
                                }
                                else if (account.Id.Equals(aa.From))
                                {
                                    account.AccountDebitedCount += 1;
                                    account.AccountDebitedVolume += aa.Amount;
                                }

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
                    $"INSERT INTO Paginations SET CursorType = 'flattenedoperation'," +
                    $" PagingToken = {databaseCommand.Operation.Id}" +
                    $" ON DUPLICATE KEY UPDATE PagingToken = IF({databaseCommand.Operation.Id} > PagingToken, '{databaseCommand.Operation.Id}', PagingToken)");

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
