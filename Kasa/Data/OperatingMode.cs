using System;

namespace Kasa;

public enum OperatingMode {

    None,
    Schedule,
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