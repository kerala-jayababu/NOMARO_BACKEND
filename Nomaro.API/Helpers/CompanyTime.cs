namespace Nomaro.API.Helpers
{
    // Returns the current wall-clock time for the company handling this request.
    // Persisted DateTime values do not carry a timezone; use this only where the
    // application explicitly requires tenant-local timestamps.
    public static class CompanyTime
    {
        private sealed record TenantTimeContext(string? TimeZoneId);

        private sealed class TimeZoneScope : IDisposable
        {
            private readonly TenantTimeContext? _previousContext;
            private bool _disposed;

            public TimeZoneScope(string? timeZoneId)
            {
                _previousContext = CurrentContext.Value;
                CurrentContext.Value = new TenantTimeContext(timeZoneId?.Trim());
            }

            public void Dispose()
            {
                if (_disposed) return;
                CurrentContext.Value = _previousContext;
                _disposed = true;
            }
        }

        private static readonly AsyncLocal<TenantTimeContext?> CurrentContext = new();

        public static IDisposable SetTimeZoneId(string? timeZoneId) => new TimeZoneScope(timeZoneId);

        public static DateTime Now
        {
            get
            {
                var timeZone = CurrentTimeZone();
                return timeZone == null
                    ? DateTime.Now
                    : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            }
        }

        public static DateTime ToLocalTime(DateTime utcDateTime)
        {
            var timeZone = CurrentTimeZone();
            if (timeZone == null)
            {
                return utcDateTime.ToLocalTime();
            }

            var utc = utcDateTime.Kind == DateTimeKind.Local
                ? utcDateTime.ToUniversalTime()
                : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        }

        private static TimeZoneInfo? CurrentTimeZone()
        {
            var context = CurrentContext.Value;
            if (context == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(context.TimeZoneId))
            {
                throw new InvalidOperationException(
                    "No timezone is configured for the current company. Set TimeZoneId in NOMARO_GLOBAL.dbo.NomaroInstanceConfig.");
            }

            try
            {
                return FindTimeZone(context.TimeZoneId);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                throw new InvalidOperationException(
                    $"The configured timezone '{context.TimeZoneId}' is invalid or unavailable on this server.", ex);
            }
        }

        private static TimeZoneInfo FindTimeZone(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                var converted = OperatingSystem.IsWindows()
                    ? TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId)
                        ? windowsId
                        : null
                    : TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var ianaId)
                        ? ianaId
                        : null;

                if (!string.IsNullOrWhiteSpace(converted))
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(converted);
                }

                throw;
            }
        }
    }
}
