using System;
using System.Configuration;
using System.Globalization;

namespace HospitalMSWebApp.Infrastructure
{
    // Presentation/input boundary only. Existing appointment and schedule storage stays UTC.
    public static class HospitalTime
    {
        public static TimeZoneInfo Zone { get { return TimeZoneInfo.FindSystemTimeZoneById(ConfigurationManager.AppSettings["HospitalTimeZoneId"] ?? "UTC"); } }
        public static DateTime Local(DateTime utc, TimeZoneInfo zone) { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone); }
        public static string Format(DateTime utc) { return Format(utc, Zone); }
        public static string Format(DateTime utc, TimeZoneInfo zone)
        {
            var instant = new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
            return TimeZoneInfo.ConvertTime(instant, zone).ToString("ddd, dd MMM yyyy · HH:mm 'UTC'zzz", CultureInfo.InvariantCulture);
        }
        public static DateTime ToUtc(DateTime local, TimeZoneInfo zone)
        {
            local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(local) || zone.IsAmbiguousTime(local)) throw new ArgumentException("This local time is skipped or repeated by a daylight-saving change. Choose another time or contact reception.");
            return TimeZoneInfo.ConvertTimeToUtc(local, zone);
        }
        public static DateTime ParseLocal(string value)
        {
            DateTime local;
            if (!DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out local)) throw new ArgumentException("Enter a valid appointment date and time.");
            return ToUtc(local, Zone);
        }
        public static DateTime ParseUtc(string value)
        {
            DateTime utc;
            if (!DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out utc)) throw new ArgumentException("The appointment selection is invalid. Reload its details.");
            return utc;
        }
        public static string Token(DateTime utc) { return DateTime.SpecifyKind(utc,DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss.fffffff'Z'",CultureInfo.InvariantCulture); }
    }
}
