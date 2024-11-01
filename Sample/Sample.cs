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
bool isPhoneCharging = await phoneCharger.System.IsOutletOn();
logger.LogInformation("Phone {is} charging", isPhoneCharging ? "is" : "is not");

using IKasaMultiOutlet ep40       = new KasaMultiOutlet("ep40.outlets.aldaviva.com", options);
SystemInfo             systemInfo = await ep40.System.GetInfo();
logger.LogInformation("{info}", systemInfo.ToString());

for (int outletId = 0; outletId < await ep40.System.CountOutlets(); outletId++) {
    bool wasOn = await ep40.System.IsOutletOn(outletId);
    await ep40.System.SetOutletOn(outletId, !wasOn);
}