﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AsaGoldBff.Helpers
{
    /// <summary>
    /// Time helpers 
    /// </summary>
    public static class TimeHelper
    {
        /// <summary>
        /// Rounds to day
        /// 
        /// Round to first hour, then rounds that hour
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long RoundDay(this DateTimeOffset time)
        {
            var date = time.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00:00+00:00";
            return DateTimeOffset.Parse(date).UtcTicks;
        }
        /// <summary>
        /// Rounds to hour
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long RoundHour(this DateTimeOffset time)
        {
            return DateTimeOffset.Parse(time.ToUniversalTime().ToString("yyyy-MM-ddTHH:00:00", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture).UtcTicks;
        }
        /// <summary>
        /// Rounds to hour
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long RoundMinute(this DateTimeOffset time)
        {
            var missingMinutes = time.Minute % 5 * -1;
            return DateTimeOffset.Parse(time.ToUniversalTime().AddMinutes(missingMinutes).ToString("yyyy-MM-ddTHH:mm:00", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture).UtcTicks;
        }
        /// <summary>
        /// Returns local offset
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static TimeSpan GetLocalOffset(this DateTimeOffset time)
        {
            return TimeZoneInfo.Local.GetUtcOffset(time.ToUniversalTime());
        }
    }
}
