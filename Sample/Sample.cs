using Kasa;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Timer = System.Threading.Timer;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder
    .ClearProviders()
    .SetMinimumLevel(LogLevel.Trace)
    .AddNLog());

ILogger logger = loggerFactory.CreateLogger("Main");

using IKasaOutlet outlet = new KasaOutlet("192.168.1.227", new Options {
    LoggerFactory = loggerFactory,
    MaxAttempts   = 1
});

int minSignalStrength = 0;

CancellationTokenSource cts = new();

Timer timer = new(async _ => {
    SystemInfo systemInfo = await outlet.System.GetInfo();
    int        rssi       = systemInfo.SignalStrength;
    logger.LogDebug("Signal strength: {rssi} dBm", rssi);

    if (rssi < minSignalStrength) {
        logger.LogInformation("New minimum signal strength: {rssi} dBm", rssi);
        minSignalStrength = rssi;
    }
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

Console.CancelKeyPress += (sender, eventArgs) => cts.Cancel();

cts.Token.WaitHandle.WaitOne();
timer.Dispose();

/*
 string         outletName         = await outlet.System.GetName();
SystemInfo     systemInfo         = await outlet.System.GetInfo();
DateTimeOffset currentDeviceTime  = await outlet.Time.GetTimeWithZoneOffset();
bool           isOutletOn         = await outlet.System.IsOutletOn();
bool           isIndicatorLightOn = await outlet.System.IsIndicatorLightOn();

logger.LogInformation("{0} - {1}", outletName, systemInfo.ModelName);
logger.LogInformation("Host: {0}", outlet.Hostname);
logger.LogInformation("Outlet state: {0}", isOutletOn ? "on" : "off");
logger.LogInformation("Time: {0:O}", currentDeviceTime);
logger.LogInformation("Hardware version: {0}", systemInfo.HardwareVersion);
logger.LogInformation("Software version: {0}", systemInfo.SoftwareVersion);
logger.LogInformation("MAC Address (RSSI): {0} ({1})", systemInfo.MacAddress, systemInfo.SignalStrength);
logger.LogInformation("Indicator light: {0}", isIndicatorLightOn ? "on" : "off");
logger.LogInformation("Mode: {0}", systemInfo.OperatingMode);

if (systemInfo.Features.Contains(Feature.EnergyMeter)) {
    PowerUsage power = await outlet.EnergyMeter.GetInstantaneousPowerUsage();
    logger.LogInformation("Energy usage: {0} mA, {1} mV, {2} mW, {3} Wh since boot", power.Current, power.Voltage, power.Power, power.CumulativeEnergySinceBoot);
} else {
    logger.LogInformation("No energy meter");
}
*/

// await outlet.Schedule.DeleteAll();

/*Schedule schedule = new(true, new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, new TimeOnly(12 + 7, 45));
schedule = await outlet.Schedule.Save(schedule);
logger.LogInformation("Created schedule with ID {Id}", schedule.Id);

IEnumerable<Schedule> schedules = await outlet.Schedule.GetAll();
foreach (Schedule s in schedules) {
    logger.LogInformation("Turn {onOff} at {time} on {days}{enabled}", s.WillSetOutletOn ? "on " : "off",
        s.TimeBasis switch {
            Schedule.Basis.StartOfDay => TimeOnly.FromTimeSpan(s.Time),
            Schedule.Basis.Sunrise    => $"{s.Time:%m} min {(s.Time < TimeSpan.Zero ? "before" : "after")} sunrise",
            Schedule.Basis.Sunset     => $"{s.Time:%m} min {(s.Time < TimeSpan.Zero ? "before" : "after")} sunset"
        }, s.IsRecurring ? string.Join(", ", s.DaysOfWeek) : s.Date, s.IsEnabled ? "" : " (disabled)");
}*/

// await outlet.Schedule.Delete(schedule);
// await outlet.Schedule.Delete(schedule.Id!);
// await outlet.Schedule.DeleteAll();




