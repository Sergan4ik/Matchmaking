public static class GlobalTime
{
    static DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    public static long seconds => (long)(dateTime - start).TotalSeconds;
    public static long ms => (long)(dateTime - start).TotalMilliseconds;
    public static DateTime dateTime => DateTime.UtcNow;
    public static DateTime ToDateTime(long time) => start.AddSeconds(time);
    public const long Day = 60 * 60 * 24;
    public const long Minute = 60;

    public static long ToTotalSeconds(this DateTime dateTime)
    {
        return (long)(dateTime - start).TotalSeconds;
    }

    public static DateTime SecondsToDate(this long value)
    {
        return start.AddSeconds(value);
    }
}