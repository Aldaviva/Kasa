namespace Kasa;

internal enum CommandFamily {

    System,
    NetworkInterface,
    Cloud,
    Time,
    EnergyMeter,
    Schedule,
    Timer,
    AwayMode

}

internal static class CommandFamilies {

    public static string ToJsonString(this CommandFamily commandFamily) => commandFamily switch {
        CommandFamily.NetworkInterface => "netif",
        CommandFamily.Cloud            => "cnCloud",
        CommandFamily.EnergyMeter      => "emeter",
        CommandFamily.Schedule         => "schedule",
        CommandFamily.Timer            => "count_down",
        CommandFamily.AwayMode         => "anti_theft",
        _                              => commandFamily.ToString().ToLowerInvariant()
    };

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Feature GetRequiredFeature(this CommandFamily commandFamily) => commandFamily switch {
        CommandFamily.Timer       => Feature.Timer,
        CommandFamily.EnergyMeter => Feature.EnergyMeter,
        _                         => throw new ArgumentOutOfRangeException(nameof(commandFamily), commandFamily, null)
    };

}