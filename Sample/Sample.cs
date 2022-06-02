using Kasa;
using Kasa.Log;
using slf4net;
using slf4net.NLog;

LoggerFactory.SetFactoryResolver(new LoggerFactoryResolver(new NLogLoggerFactory()));
ILogger logger = LoggerFactory.GetLogger("Main");

using KasaOutlet outlet = new("192.168.1.227");
await outlet.Connect();

// await outlet.System.SetIndicatorLightOn(true);

// SystemInfo   systemInfo         = await outlet.System.GetInfo();
// DateTime     currentDeviceTime  = await outlet.Time.GetTime();
// TimeZoneInfo timeZoneInfo       = (await outlet.Time.GetTimeZones()).First();
// bool         isOutletOn         = await outlet.System.IsOutletOn();
// bool         isIndicatorLightOn = await outlet.System.IsIndicatorLightOn();
// PowerUsage power = await outlet.EnergyMeter.GetInstantaneousPowerUsage();
//
// logger.Info("{0} - {1}", systemInfo.Name, systemInfo.ModelName);
// logger.Info("Host: {0}", outlet.Hostname);
// logger.Info("Outlet state: {0}", isOutletOn ? "on" : "off");
// logger.Info("Time: {0:G} ({1})", currentDeviceTime, timeZoneInfo.IsDaylightSavingTime(currentDeviceTime) ? timeZoneInfo.DaylightName : timeZoneInfo.StandardName);
// logger.Info("Hardware version: {0}", systemInfo.HardwareVersion);
// logger.Info("Software version: {0}", systemInfo.SoftwareVersion);
// logger.Info("MAC Address (RSSI): {0} ({1})", systemInfo.MacAddress, systemInfo.Rssi);
// logger.Info("Indicator light: {0}", isIndicatorLightOn ? "on" : "off");
// logger.Info("Mode: {0}", systemInfo.OperatingMode);
// logger.Info("Energy usage: {0} mA, {1} mV, {2} mW, {3} Wh since boot", power.Current, power.Voltage, power.Power, power.CumulativeEnergySinceBoot);

// Timer timer = new(1000);
// timer.Elapsed += async delegate {
//     DateTime currentDeviceTime = await outlet.Time.GetTime();
//     logger.Info("Time: {0:G}", currentDeviceTime);
// };
// timer.Start();

// Socket.ConnectAsync()

CancellationTokenSource interrupted = new();
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
outlet.Dispose();