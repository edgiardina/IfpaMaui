using System;

namespace Ifpa.Models
{
    public static class IntExtensions
    {
        public static TimeSpan Milliseconds(this int thisInt)
        {
            return new TimeSpan(0, 0, 0, 0, thisInt);
        }

        public static TimeSpan Millisecond(this int thisInt)
        {
            return Milliseconds(thisInt);
        }

        public static TimeSpan Seconds(this int thisInt)
        {
            return new TimeSpan(0, 0, thisInt);
        }

        public static TimeSpan Second(this int thisInt)
        {
            return Seconds(thisInt);
        }

        public static TimeSpan Minutes(this int thisInt)
        {
            return new TimeSpan(0, thisInt, 0);
        }

        public static TimeSpan Minute(this int thisInt)
        {
            return Minutes(thisInt);
        }

        public static TimeSpan Hours(this int thisInt)
        {
            return new TimeSpan(thisInt, 0, 0);
        }

        public static TimeSpan Hour(this int thisInt)
        {
            return Hours(thisInt);
        }

        public static TimeSpan Days(this int thisInt)
        {
            return new TimeSpan(thisInt, 0, 0, 0);
        }

        public static TimeSpan Day(this int thisInt)
        {
            return Days(thisInt);
        }

        public static DateTime MinutesAgo(this int thisInt)
        {
            return DateTime.UtcNow.AddMinutes(thisInt * -1);
        }

        public static DateTime MinuteAgo(this int thisInt)
        {
            return DateTime.UtcNow.AddMinutes(thisInt * -1);
        }

        public static DateTime SecondsAgo(this int thisInt)
        {
            return DateTime.UtcNow.AddSeconds(thisInt * -1);
        }

        public static DateTime SecondAgo(this int thisInt)
        {
            return DateTime.UtcNow.AddSeconds(thisInt * -1);
        }

        public static DateTime HoursAgo(this int thisInt)
        {
            return DateTime.UtcNow.AddHours(thisInt * -1);
        }

        public static DateTime HourAgo(this int thisInt)
        {
            return DateTime.UtcNow.AddHours(thisInt * -1);
        }

        public static DateTime DaysAgo(this int thisInt)
        {
            return DateTime.UtcNow.AddDays(thisInt * -1);
        }

        public static DateTime DayAgo(this int thisInt)
        {
            return DateTime.UtcNow.AddDays(thisInt * -1);
        }

        public static DateTime MinutesFromNow(this int thisInt)
        {
            return DateTime.UtcNow.AddMinutes(thisInt);
        }

        public static DateTime MinuteFromNow(this int thisInt)
        {
            return DateTime.UtcNow.AddMinutes(thisInt);
        }

        public static DateTime SecondsFromNow(this int thisInt)
        {
            return DateTime.UtcNow.AddSeconds(thisInt);
        }

        public static DateTime SecondFromNow(this int thisInt)
        {
            return DateTime.UtcNow.AddSeconds(thisInt);
        }

        public static DateTime HoursFromNow(this int thisInt)
        {
            return DateTime.UtcNow.AddHours(thisInt);
        }

        public static DateTime HourFromNow(this int thisInt)
        {
            return DateTime.UtcNow.AddHours(thisInt);
        }

        public static DateTime DaysFromNow(this int thisInt)
        {
            return DateTime.UtcNow.AddDays(thisInt);
        }

        public static DateTime DayFromNow(this int thisInt)
        {
            return DateTime.UtcNow.AddDays(thisInt);
        }

    }
}
