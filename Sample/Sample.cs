using Kasa;
using Kasa.Logging;
using slf4net;
using slf4net.NLog;

LoggerFactory.SetFactoryResolver(new LoggerFactoryResolver(new NLogLoggerFactory()));
ILogger logger = LoggerFactory.GetLogger("Main");

using KasaOutlet outlet = new("sx20.outlets.aldaviva.com");
await outlet.Connect();

// await outlet.System.SetIndicatorLightOn(true);

SystemInfo   systemInfo         = await outlet.System.GetSystemInfo();
DateTime     currentDeviceTime  = await outlet.Time.GetTime();
TimeZoneInfo timeZoneInfo       = (await outlet.Time.GetTimeZones()).First();
bool         isOutletOn         = await outlet.System.IsOutletOn();
bool         isIndicatorLightOn = await outlet.System.IsIndicatorLightOn();

logger.Info("{0} - {1}", systemInfo.Name, systemInfo.ModelName);
logger.Info("Host: {0}", outlet.Hostname);
logger.Info("Outlet state: {0}", isOutletOn ? "on" : "off");
logger.Info("Time: {0:G} ({1})", currentDeviceTime, timeZoneInfo.IsDaylightSavingTime(currentDeviceTime) ? timeZoneInfo.DaylightName : timeZoneInfo.StandardName);
logger.Info("Hardware version: {0}", systemInfo.HardwareVersion);
logger.Info("Software version: {0}", systemInfo.SoftwareVersion);
logger.Info("MAC Address (RSSI): {0} ({1})", systemInfo.MacAddress, systemInfo.Rssi);
logger.Info("Indicator light: {0}", isIndicatorLightOn ? "on" : "off");
logger.Info("Mode: {0}", systemInfo.OperatingMode);