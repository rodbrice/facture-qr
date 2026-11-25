namespace FactureQr.Api.Infrastructure;

/// <summary>
/// Today's date in Switzerland. Issue, due and payment dates are calendar dates
/// in Swiss local time, whatever the time zone of the server.
/// </summary>
public sealed class SwissCalendar
{
    private static readonly TimeZoneInfo Zurich = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

    private readonly TimeProvider clock;

    /// <summary>Creates a calendar that reads the time from the given clock.</summary>
    public SwissCalendar(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        this.clock = clock;
    }

    /// <summary>Today's date in Europe/Zurich.</summary>
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Zurich).DateTime);
}
