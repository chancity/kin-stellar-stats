using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using discord_web_hook_logger;
using Kin.Horizon.Api.Poller.Database.StellarObjectWrappers;
using Microsoft.Extensions.Logging;

namespace Kin.Horizon.Api.Poller.Services.Model
{
    internal class StatsManager
    {
        public DateTime? CurrentDate => _currentDate;

        public ConcurrentDictionary<DateTime, DailyStats> DailyStats { get; }
        private readonly IDiscordLogger _logger;
        private DateTime? _currentDate;

        public StatsManager()
        {
            DailyStats = new ConcurrentDictionary<DateTime, DailyStats>();
            _logger = DicordLogFactory.GetLogger<StatsManager>(GlobalVariables.DiscordId, GlobalVariables.DiscordToken);
        }

        public void HandleOperation(FlattenedOperation operation)
        {
            if (CurrentDate == null || operation.CreatedAt > _currentDate)
                _currentDate = operation.CreatedAt.Date;

            var dailyStats = DailyStats.GetOrAdd(operation.CreatedAt.Date, new DailyStats(operation.CreatedAt.Date));
            dailyStats.HandleOperation(operation);
        }

        public void OutPutToDiscord()
        {
            if (_currentDate != null && DailyStats.TryGetValue(_currentDate.Value, out var today))
            {
                _logger.LogInformation(today.ToString());

                foreach (KeyValuePair<string, AppStats> todayAppStat in today.AppStats)
                {
                    _logger.LogInformation(todayAppStat.Value.ToString());
                }
            }

        }
    }

    internal class DailyStats : Stats
    {
        public DateTime Date { get; set; }
        public HashSet<string> ActiveWallets { get; }
        public ConcurrentDictionary<string, AppStats> AppStats { get; }
        private readonly object _activeUsersLockObject;

        public DailyStats(DateTime date)
        {
            Date = date;
            _activeUsersLockObject = new object();
            ActiveWallets = new HashSet<string>();
            AppStats = new ConcurrentDictionary<string, AppStats>();
        }

        internal void HandleOperation(FlattenedOperation operation)
        {
            string appId = "unknown";

            if (!string.IsNullOrEmpty(operation.Memo))
            {
                var appIdSplit = operation.Memo.Split('-');

                if (appIdSplit.Length >= 2)
                {
                    appId = appIdSplit[1];
                }
            }

            var activeAccounts = 0;
            if (operation is FlattenPaymentOperation po)
            {
                lock (_activeUsersLockObject)
                {
                    if (ActiveWallets.Add(po.From))
                        activeAccounts++;

                    if (ActiveWallets.Add(po.To))
                        activeAccounts++;
                }
            }

            var appStats = AppStats.GetOrAdd(appId, new AppStats(appId));

            HandleOperationStats(operation, activeAccounts);

            appStats.HandleOperationStats(operation, activeAccounts);
        }

        public override string ToString()
        {
            return $"Aggregated Stats\n\n" +
                   $"{base.ToString()}";
        }
    }

    internal class AppStats : Stats
    {
        public string AppId { get; }

        public AppStats(string appId)
        {
            AppId = appId;
        }

        public override string ToString()
        {
            return $"AppId: {AppId}\n\n" +
                   $"{base.ToString()}";
        }
    }

    internal class Stats
    {
        private long _activeUserCount;
        private long _createdWalletsCount;
        private long _operationCount;
        private long _paymentCount;
        private long _paymentVolume;

        public long ActiveUserCount => _activeUserCount;

        public long OperationCount => _operationCount;

        public long PaymentCount => _paymentCount;

        public long PaymentVolume => _paymentVolume;

        public long CreatedWalletsCount => _createdWalletsCount;

        internal void HandleOperationStats(FlattenedOperation operation, int activeUsers = 0)
        {
            Interlocked.Increment(ref _operationCount);
            Interlocked.Add(ref _activeUserCount, activeUsers);

            if (operation is FlattenPaymentOperation po)
            {
                Interlocked.Increment(ref _paymentCount);
                Interlocked.Add(ref _paymentVolume, (long) po.Amount);
            }
            else if (operation is FlattenCreateAccountOperation)
            {
                Interlocked.Increment(ref _createdWalletsCount);
            }
        }

        public override string ToString()
        {
            return $"ActiveUserCount: {ActiveUserCount}\n" +
                   $"OperationCount: {OperationCount}\n" +
                   $"PaymentCount: {PaymentCount}\n" +
                   $"PaymentVolume: {PaymentVolume}\n" +
                   $"CreatedWalletsCount: {CreatedWalletsCount}";
        }
    }
}
