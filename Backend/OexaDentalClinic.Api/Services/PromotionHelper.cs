using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Services
{
    public static class PromotionHelper
    {
        /// <summary>Clinic operates in Albania — use local calendar date for active checks.</summary>
        public static DateTime ClinicToday
        {
            get
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Tirane");
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
                }
                catch
                {
                    return DateTime.Today;
                }
            }
        }

        public static DateTime NormalizeDate(DateTime d) => d.Date;

        public static bool IsActiveOnDate(Promotion p, DateTime? onDate = null)
        {
            if (!p.IsActive || string.IsNullOrWhiteSpace(p.ProblemKey))
                return false;

            var day = (onDate ?? ClinicToday).Date;
            var start = p.StartDate.Date;
            var end = p.EndDate.Date;
            return start <= day && end >= day;
        }

        public static bool KeysMatch(string? promoKey, string problemKey)
        {
            return string.Equals(promoKey?.Trim(), problemKey.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
