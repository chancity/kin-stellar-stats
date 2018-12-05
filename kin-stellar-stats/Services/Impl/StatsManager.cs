using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using discord_web_hook_logger;
using Kin.Horizon.Api.Poller.Database;
using Kin.Horizon.Api.Poller.Database.Helpers;
using Kin.Horizon.Api.Poller.Services.Model;
using Kin.Stellar.Sdk.responses.operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kin.Horizon.Api.Poller.Services.Impl
{
    internal class StatsManager
    {
        private readonly KinstatsContext _kinstatsContext;
        private readonly IDiscordLogger _logger;
        private readonly Dictionary<string, int> AppMap;
        private DateTime? _currentDate;
        public ConcurrentDictionary<DateTime, DailyStats> DailyStats { get; }

        public StatsManager(KinstatsContext managementContext)
        {
            _kinstatsContext = managementContext;
            DailyStats = new ConcurrentDictionary<DateTime, DailyStats>();
            _logger = DicordLogFactory.GetLogger<StatsManager>(GlobalVariables.DiscordId, GlobalVariables.DiscordToken);
            AppMap = new Dictionary<string, int>();
        }

        public async Task<bool> PopulateSavedActiveWallets(OperationResponse operation)
        {
            DateTime createdAt = DateTimeOffset.Parse(operation.CreatedAt).Date;

            if (!DailyStats.ContainsKey(createdAt))
            {
                DailyStats dailyStats = DailyStats.GetOrAdd(createdAt, new DailyStats(createdAt));
                await PopulateSavedActiveWallets(dailyStats);
                _logger.LogInformation($"Populated saved wallets with {dailyStats.ActiveWalletsSaved.Count} entries");
            }

            return true;
        }

        public async Task<bool> SaveData()
        {

            _logger.LogInformation("Blocking operation handler to save data");

            try
            {
                if (!DailyStats.IsEmpty)
                {
                    foreach (DailyStats dailyStatsValue in DailyStats.Values)
                    {
                        foreach (AppStats appStats in dailyStatsValue.AppStats.Values)
                        {
                            App app = await _kinstatsContext.App.SingleOrDefaultAsync(x => x.AppId == appStats.AppId);

                            if (app == null)
                            {
                                app = new App {AppId = appStats.AppId};
                                await _kinstatsContext.App.AddAsync(app);
                                await _kinstatsContext.SaveChangesAsync();
                            }

                            Database.AppStats dbAppStats = await _kinstatsContext.AppStats.SingleOrDefaultAsync(x => x.Year == dailyStatsValue.Date.Year && x.Day == dailyStatsValue.Date.DayOfYear && x.AppId == app.Id);

                            if (dbAppStats == null)
                            {
                                dbAppStats = new Database.AppStats
                                {
                                    ActiveUsers = appStats.ActiveUserCount,
                                    CreatedWallets = appStats.CreatedWalletsCount,
                                    Operations = appStats.OperationCount,
                                    Payments = appStats.PaymentCount,
                                    PaymentVolume = appStats.PaymentVolume,
                                    AppId = app.Id,
                                    Day = (ushort) dailyStatsValue.Date.DayOfYear,
                                    Year = (ushort) dailyStatsValue.Date.Year
                                };
                                app.AppStats.Add(dbAppStats);
                            }
                            else
                            {
                                dbAppStats.ActiveUsers += appStats.ActiveUserCount;
                                dbAppStats.CreatedWallets += appStats.CreatedWalletsCount;
                                dbAppStats.Operations += appStats.OperationCount;
                                dbAppStats.Payments += appStats.PaymentCount;
                                dbAppStats.PaymentVolume += appStats.PaymentVolume;
                            }

                            OverallStats dbOverallStats = await _kinstatsContext.OverallStats.SingleOrDefaultAsync(x => x.AppId == app.Id);

                            if (dbOverallStats == null)
                            {
                                dbOverallStats = new OverallStats
                                {
                                    ActiveUsers = appStats.ActiveUserCount,
                                    CreatedWallets = appStats.CreatedWalletsCount,
                                    Operations = appStats.OperationCount,
                                    Payments = appStats.PaymentCount,
                                    PaymentVolume = appStats.PaymentVolume,
                                    AppId = app.Id
                                };
                                app.OverallStats = dbOverallStats;
                            }
                            else
                            {
                                dbOverallStats.ActiveUsers += appStats.ActiveUserCount;
                                dbOverallStats.CreatedWallets += appStats.CreatedWalletsCount;
                                dbOverallStats.Operations += appStats.OperationCount;
                                dbOverallStats.Payments += appStats.PaymentCount;
                                dbOverallStats.PaymentVolume += appStats.PaymentVolume;
                            }
                        }

                        if (dailyStatsValue.ActiveWalletsNotSaved.Count > 0)
                        {

                            foreach (var wallet in dailyStatsValue.ActiveWalletsNotSaved)
                            {
                                await _kinstatsContext.Database.ExecuteSqlCommandAsync($"INSERT IGNORE INTO active_wallet (year, day, address) VALUES ({dailyStatsValue.Date.Year}, {dailyStatsValue.Date.DayOfYear}, '{wallet}')");
                            }

                        }
                    }

                    var cursorPage = await _kinstatsContext.Pagination.SingleOrDefaultAsync(x => x.CursorType == "operation");
                    var ds = DailyStats.Values.Last();

                    if (cursorPage == null)
                    {
                        _kinstatsContext.Pagination.Add(new Pagination()
                            {CursorId = (ulong) ds.CurrentPagingToken, CursorType = "operation"});
                    }
                    else
                    {
                        cursorPage.CursorId = (ulong) ds.CurrentPagingToken;
                    }

                    await _kinstatsContext.SaveChangesAsync();

                    foreach (DailyStats dailyStatsValue in DailyStats.Values)
                    {
                        dailyStatsValue.AddSavedActiveWallets(dailyStatsValue.ActiveWalletsNotSaved.ToList());
                    }

                    ClearData();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
            finally
            {
                _logger.LogInformation("Finished saving data");
                _logger.LogInformation(Impl.DailyStats.AggregatedStatsToString());
            }

            return true;
        }

        public void ClearData()
        {
            if (DailyStats.Count > 3)
            {
                DateTime firstKey = DailyStats.Keys.FirstOrDefault();

                if (DailyStats.TryRemove(firstKey, out DailyStats day))
                {
                    day.ClearActiveWallets();
                    day.AppStats.Clear();
                }
            }

            foreach (DailyStats dailyStatsValue in DailyStats.Values)
            {
                dailyStatsValue.Clear();
            }
        }

        private async Task<bool> PopulateSavedActiveWallets(DailyStats dailyStats)
        {
            if (dailyStats.ActiveWalletsSaved.Count == 0)
            {
                IQueryable<string> query = _kinstatsContext.ActiveWallet.AsNoTracking()
                    .Where(x => x.Year == dailyStats.Date.Year && x.Day == dailyStats.Date.DayOfYear).Select(x => x.Address);

                PaginatedList<string> activeWallets = await PaginatedList<string>.CreateAsync(query, 1, 200);

                if (activeWallets.TotalCount > 0)
                {
                    dailyStats.AddSavedActiveWallets(activeWallets);

                    while (activeWallets.HasNextPage)
                    {
                        activeWallets = await PaginatedList<string>.CreateAsync(query, activeWallets.PageIndex + 1, 200, activeWallets.Count);
                        dailyStats.AddSavedActiveWallets(activeWallets);
                    }
                }

                return true;
            }

            return true;
        }

        public void HandleOperation(FlattenedOperation operation)
        {
            if (_currentDate == null || operation.CreatedAt.Date > _currentDate)
            {
                _currentDate = operation.CreatedAt.Date;
            }

            DailyStats dailyStats =
                DailyStats.GetOrAdd(operation.CreatedAt.Date, new DailyStats(operation.CreatedAt.Date));

            dailyStats.HandleOperation(operation);
        }
    }

    internal class DailyStats
    {
        public static Stats SinceRunTimeStats;

        internal long CurrentPagingToken = long.MinValue;
        private readonly object _activeUsersLockObject;
        public DateTime Date { get; set; }
        public HashSet<string> ActiveWalletsSaved { get; }
        public HashSet<string> ActiveWalletsNotSaved { get; }
        public ConcurrentDictionary<string, AppStats> AppStats { get; }

        static DailyStats()
        {
            SinceRunTimeStats = new Stats();
        }
        public DailyStats(DateTime date)
        {
            Date = date;
            _activeUsersLockObject = new object();
            ActiveWalletsSaved = new HashSet<string>();
            ActiveWalletsNotSaved = new HashSet<string>();
            AppStats = new ConcurrentDictionary<string, AppStats>();
        }

        internal void HandleOperation(FlattenedOperation operation)
        {

            if (operation.Id > CurrentPagingToken)
            {
                CurrentPagingToken = operation.Id;
            }

            string appId = "unknown";

            if (!string.IsNullOrEmpty(operation.Memo))
            {
                string[] appIdSplit = operation.Memo.Split('-');

                if (appIdSplit.Length >= 2)
                {
                    appId = appIdSplit[1];
                }
            }

            int activeAccounts = 0;

            if (operation is FlattenPaymentOperation po)
            {
                lock (_activeUsersLockObject)
                {
                    if (!ActiveWalletsSaved.Contains(po.From) && ActiveWalletsNotSaved.Add(po.From))
                    {
                        activeAccounts++;
                    }


                    if (!ActiveWalletsSaved.Contains(po.To) && ActiveWalletsNotSaved.Add(po.To))
                    {
                        activeAccounts++;
                    }
                }
            }

            AppStats aggregatedAppStats = AppStats.GetOrAdd("aggregated", new AppStats("aggregated"));
            AppStats appStats = AppStats.GetOrAdd(appId, new AppStats(appId));

            SinceRunTimeStats.HandleOperationStats(operation,activeAccounts);
            aggregatedAppStats.HandleOperationStats(operation, activeAccounts);
            appStats.HandleOperationStats(operation, activeAccounts);
        }

        internal void Clear()
        {
            AppStats.Clear();

            lock (_activeUsersLockObject)
            {
                ActiveWalletsNotSaved.Clear();
            }
        }

        internal void ClearActiveWallets()
        {
            lock (_activeUsersLockObject)
            {
                ActiveWalletsSaved.Clear();
            }
        }

        internal void AddSavedActiveWallets(List<string> wallets)
        {
            lock (_activeUsersLockObject)
            {
                foreach (string activeWallet in wallets)
                {
                    ActiveWalletsSaved.Add(activeWallet);
                }
            }
        }

        public static string AggregatedStatsToString()
        {
            return $"\nAggregated Stats\n" +
                   $"{SinceRunTimeStats.ToString()}";
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
            return $"\nAppId: {AppId}\n" +
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