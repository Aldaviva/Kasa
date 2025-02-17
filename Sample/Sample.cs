using Kasa;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder
    .ClearProviders()
    .SetMinimumLevel(LogLevel.Trace) // control actual level in NLog.config
    .AddNLog());
ILogger logger  = loggerFactory.CreateLogger("Sample");
Options options = new() { MaxAttempts = 1, LoggerFactory = loggerFactory };

using IKasaOutlet washingMachine = new KasaOutlet("washingmachine.outlets.aldaviva.com", options);
logger.LogInformation("{info}", (await washingMachine.System.GetInfo()).ToString());
PowerUsage usage = await washingMachine.EnergyMeter.GetInstantaneousPowerUsage();
logger.LogInformation("Washing machine is currently drawing {a:N1} amps, {w:N1} watts", usage.Current / 1000.0, usage.Power / 1000.0);

using IMultiSocketKasaOutlet ep40       = new MultiSocketKasaOutlet("192.168.1.189", options);
SystemInfo                   systemInfo = await ep40.System.GetInfo();
logger.LogInformation("{info}", systemInfo.ToString());
for (int outletId = 0; outletId < await ep40.System.CountSockets(); outletId++) {
    bool wasOn = await ep40.System.IsSocketOn(outletId);
    await ep40.System.SetSocketOn(outletId, !wasOn);
}