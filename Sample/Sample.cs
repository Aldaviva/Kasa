using Kasa;
using Kasa.Logging;
using slf4net;
using slf4net.NLog;

LoggerFactory.SetFactoryResolver(new LoggerFactoryResolver(new NLogLoggerFactory()));
ILogger logger = LoggerFactory.GetLogger("Main");

using KasaSmartOutlet outlet = new("sx20.outlets.aldaviva.com");
await outlet.Connect();

// await outlet.System.SetIndicatorLightOn(true);

SystemInfo systemInfo = await outlet.System.GetSystemInfo();
logger.Info("{0} - {1}", systemInfo.Name, systemInfo.ModelId);
logger.Info("Host: {0}", outlet.Hostname);
logger.Info("Outlet state: {0}", await outlet.System.IsOutletOn() ? "on" : "off");
logger.Info("Time: {0:G}", await outlet.Time.GetTime());
logger.Info("Hardware version: {0}", systemInfo.HardwareVersion);
logger.Info("Software version: {0}", systemInfo.SoftwareVersion);
logger.Info("MAC Address (RSSI): {0} ({1})", systemInfo.MacAddress, systemInfo.Rssi);
logger.Info("Indicator light: {0}", await outlet.System.IsIndicatorLightOn() ? "on" : "off");
logger.Info("Mode: {0}", systemInfo.OperatingMode);