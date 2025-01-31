using Kasa;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder
    .ClearProviders()
    .SetMinimumLevel(LogLevel.Trace)
    .AddNLog());
ILogger logger  = loggerFactory.CreateLogger("Sample");
Options options = new() { MaxAttempts = 1, LoggerFactory = loggerFactory };

using IKasaOutlet phoneCharger = new KasaOutlet("phonecharger.outlets.aldaviva.com", options);
logger.LogInformation("{info}", (await phoneCharger.System.GetInfo()).ToString());
bool isPhoneCharging = await phoneCharger.System.IsSocketOn();
logger.LogInformation("Phone {is} charging", isPhoneCharging ? "is" : "is not");

using IMultiSocketKasaOutlet ep40       = new MultiSocketKasaOutlet("192.168.1.189", options);
SystemInfo                   systemInfo = await ep40.System.GetInfo();
logger.LogInformation("{info}", systemInfo.ToString());
for (int outletId = 0; outletId < await ep40.System.CountSockets(); outletId++) {
    bool wasOn = await ep40.System.IsSocketOn(outletId);
    await ep40.System.SetSocketOn(outletId, !wasOn);
}