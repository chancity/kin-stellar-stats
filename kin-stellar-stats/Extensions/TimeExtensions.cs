using System;
using System.Collections.Generic;
using System.Text;

namespace Kin.Horizon.Api.Poller.Extensions
{
    internal static class TimeExtensions
    {
        public static DateTime FromUnixTime(this long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static long ToEpochNearestHour(this DateTimeOffset date)
        {
            long roundTicks = TimeSpan.TicksPerHour;
            return new DateTimeOffset(new DateTime(date.Ticks - date.Ticks % roundTicks)).ToUnixTimeSeconds();
        }

    }
}
