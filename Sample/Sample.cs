using System.Net.Sockets;
using Kasa;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

// LoggerFactory.SetFactoryResolver(new LoggerFactoryResolver(new NLogLoggerFactory()));
// ILogger logger = LoggerFactory.GetLogger("Main");

ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder
    .ClearProviders()
    .SetMinimumLevel(LogLevel.Trace)
    .AddNLog());

ILogger logger = loggerFactory.CreateLogger("Main");

using IKasaOutlet outlet = new KasaOutlet("192.168.1.227") {
    LoggerFactory  = loggerFactory,
    SendTimeout    = TimeSpan.FromSeconds(2),
    ReceiveTimeout = TimeSpan.FromSeconds(2),
    MaxAttempts    = 2
};

try {
    await outlet.Connect();
} catch (SocketException e) {
    Console.WriteLine($"Failed to connect due to SocketException: {e.Message}");
    return;
} catch (Exception e) {
    Console.WriteLine($"Failed to connect due to {e.GetType().Name}: {e.Message}");
    return;
}

// async void Callback(object? state) {
//     PowerUsage usage = await outlet.EnergyMeter.GetInstantaneousPowerUsage();
//     logger.LogInformation("Current: {current} mA; Voltage: {voltage} mV; Power: {power} mW", usage.Current, usage.Voltage, usage.Power);
//     // await outlet.System.SetIndicatorLightOn(isLightOn ^= true);
// }
//
// await using Timer timer = new(Callback, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
//
// using CancellationTokenSource interrupted = new();
// Console.CancelKeyPress += (sender, eventArgs) => {
//     eventArgs.Cancel = true;
//     interrupted.Cancel();
// };
//
// interrupted.Token.WaitHandle.WaitOne();

// await outlet.System.SetIndicatorLightOn(true);
// await Task.Delay(750);
// await outlet.System.SetIndicatorLightOn(false);
// await Task.Delay(750);
// await outlet.System.SetIndicatorLightOn(true);

SystemInfo     systemInfo        = await outlet.System.GetInfo();
DateTimeOffset currentDeviceTime = await outlet.Time.GetTimeWithZoneOffset();
// TimeZoneInfo timeZoneInfo       = (await outlet.Time.GetTimeZones()).First();
bool isOutletOn         = await outlet.System.IsOutletOn();
bool isIndicatorLightOn = await outlet.System.IsIndicatorLightOn();
// PowerUsage   power              = await outlet.EnergyMeter.GetInstantaneousPowerUsage();

logger.LogInformation("{0} - {1}", systemInfo.Name, systemInfo.ModelName);
logger.LogInformation("Host: {0}", outlet.Hostname);
logger.LogInformation("Outlet state: {0}", isOutletOn ? "on" : "off");
logger.LogInformation("Time: {0:O}", currentDeviceTime);
logger.LogInformation("Hardware version: {0}", systemInfo.HardwareVersion);
logger.LogInformation("Software version: {0}", systemInfo.SoftwareVersion);
logger.LogInformation("MAC Address (RSSI): {0} ({1})", systemInfo.MacAddress, systemInfo.Rssi);
logger.LogInformation("Indicator light: {0}", isIndicatorLightOn ? "on" : "off");
logger.LogInformation("Mode: {0}", systemInfo.OperatingMode);
// logger.LogInformation("Energy usage: {0} mA, {1} mV, {2} mW, {3} Wh since boot", power.Current, power.Voltage, power.Power, power.CumulativeEnergySinceBoot);

/*CancellationTokenSource interrupted = new();
while (!interrupted.IsCancellationRequested) {
    try {
        DateTime currentDeviceTime = await outlet.Time.GetTime();
        logger.Info("Time: {0:G}", currentDeviceTime);
    } catch (IOException e) {
        logger.Error(e, "Failed to get time, waiting and trying again...");
    }

    await Task.Delay(1000);
}

Console.CancelKeyPress += (sender, eventArgs) => {
    eventArgs.Cancel = true;
    interrupted.Cancel();
};

interrupted.Token.WaitHandle.WaitOne();
// timer.Stop();
outlet.Dispose();*/