using BOOKLY.Domain.Interfaces;

namespace BOOKLY.Infrastructure.Time
{
    public sealed class DateTimeProvider : IDateTimeProvider
    {
        private static readonly TimeZoneInfo ArgentinaTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(GetArgentinaTimeZoneId());

        public DateTime NowArgentina()
        {
            var argentinaNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ArgentinaTimeZone);
            return DateTime.SpecifyKind(argentinaNow, DateTimeKind.Unspecified);
        }

        public DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        private static string GetArgentinaTimeZoneId()
        {
            return OperatingSystem.IsWindows()
                ? "Argentina Standard Time"
                : "America/Argentina/Buenos_Aires";
        }
    }
}
