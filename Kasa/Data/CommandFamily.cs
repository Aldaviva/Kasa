namespace Kasa;

internal enum CommandFamily {

    System,
    NetworkInterface,
    Cloud,
    Time,
    EnergyMeter,
    Scheduling,
    Timer,
    AwayMode

}

internal static class CommandFamilies {

    public static string ToJsonString(this CommandFamily commandFamily) => commandFamily switch {
        CommandFamily.NetworkInterface => "netif",
        CommandFamily.Cloud            => "cnCloud",
        CommandFamily.EnergyMeter      => "emeter",
        CommandFamily.Scheduling       => "schedule",
        CommandFamily.Timer            => "count_down",
        CommandFamily.AwayMode         => "anti_theft",
        _                              => commandFamily.ToString().ToLowerInvariant()
    };

}