namespace Kasa;

/// <summary>
/// How the Kasa device has been configured to run. This corresponds to what you've selected in the Kasa mobile app.
/// </summary>
public enum OperatingMode {

    /// <summary>
    /// The outlet will not automatically turn on or off, but you can still control it remotely. THis is the default mode if you haven't explicitly configured a schedule or timer.
    /// </summary>
    None,

    /// <summary>
    /// The outlet will turn on or off with weekly recurrence according to the schedule you have defined, for example, turn on at 9 AM every weekday. Can also include one-time schedules, such as turn on once at 9 AM this Monday.
    /// </summary>
    Schedule,

    /// <summary>
    /// The outlet will turn on or off after a period that you have specified, for example, turn on after 2 hours.
    /// </summary>
    Timer

}

internal static class OperatingModes {

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static OperatingMode FromJsonString(string jsonString) => jsonString.ToLowerInvariant() switch {
        "none"       => OperatingMode.None,
        "schedule"   => OperatingMode.Schedule,
        "count_down" => OperatingMode.Timer,
        _            => throw new ArgumentOutOfRangeException(nameof(jsonString), jsonString, "Unknown feature")
    };

}