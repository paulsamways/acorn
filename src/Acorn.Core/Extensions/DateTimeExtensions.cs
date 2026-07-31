namespace Acorn.Core.Extensions;

public static class DateTimeExtensions
{
  public static DateTimeOffset ConvertUtcToLocal(this DateTime utcTime, TimeZoneInfo timeZone)
  {
    var userLocalTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);
    var offset = timeZone.GetUtcOffset(userLocalTime);
    return new DateTimeOffset(userLocalTime, offset);
  }
}
