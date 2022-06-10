using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kasa;

/// <summary>
/// <para>A TP-Link Kasa outlet or plug. This interface is the main entry point of the Kasa library. The corresponding implementation is <see cref="KasaOutlet"/>.</para>
/// <para>You may optionally call <see cref="Connect"/> on each instance before using it. If you don't, it will connect automatically when sending the first command.</para>
/// <para>Remember to <c>Dispose</c> each instance when you're done using it in order to close the TCP connection with the device. Disposed instances may not be reused, even if you call <see cref="Connect"/> again.</para>
/// <para>To communicate with multiple Kasa devices, construct multiple <see cref="KasaOutlet"/> instances, one per device.</para>
/// <para>Example usage:</para>
/// <code>using IKasaOutlet outlet = new KasaOutlet("192.168.1.100");
/// bool isOutletOn = await outlet.System.IsOutletOn();
/// if(!isOutletOn){
///     await outlet.System.SetOutletOn(true);
/// }</code>
/// </summary>
public interface IKasaOutlet: IOptions, IDisposable {

    /// <summary>
    /// The hostname that you specified for the client when you constructed the <see cref="KasaOutlet"/>. Can be an IP address or FQDN.
    /// </summary>
    string Hostname { get; }

    /// <summary>
    /// <para>Connects to the outlet using the given hostname.</para>
    /// <para>You may optionally call this to explicitly connect before sending any commands on the outlet.</para>
    /// <para>If you don't call this method before sending a command (such as <c>IKasaOutlet.System.GetInfo()</c>), then this instance will automatically connect before sending that command.</para>
    /// <para>Explicit connection may be more helpful for early detection of errors, as well as for reducing the latency of the first command. Automatic connection may be more convenient because there are fewer methods to invoke.</para>
    /// <para>If this instance is already connected, then this call does nothing and returns immediately.</para>
    /// </summary>
    /// <exception cref="ObjectDisposedException">This instance has already been disposed.</exception>
    /// <exception cref="NetworkException">The TCP connection failed.</exception>
    Task Connect();

    /// <summary>
    /// Commands that get or set system properties, like status, name, and whether the outlet is on or off.
    /// </summary>
    ISystemCommands System { get; }

    /// <summary>
    /// <para>Commands that deal with the outlet's internal clock that keeps track of the current date and time.</para>
    /// <para>This is unrelated to schedules and timers that control when the outlet turns or off.</para>
    /// </summary>
    ITimeCommands Time { get; }

    /// <summary>
    /// <para>Commands that deal with the energy consumption of the electrical consumers attached to the outlet.</para>
    /// <para>These commands are not available on all Kasa devices – they require the <see cref="Feature.EnergyMeter"/> feature, which is only available on models like KP125, KP115, HS300, and HS110.</para>
    /// <para>To check if your device has this feature and can handle these commands, you can call <c>(await IKasaOutlet.System.GetInfo()).Features.Contains(Features.EnergyMeter)</c>.</para>
    /// </summary>
    IEnergyMeterCommands EnergyMeter { get; }

    /// <summary>
    /// Commands that get or set system properties, like status, name, and whether the outlet is on or off.
    /// </summary>
    public interface ISystemCommands {

        /// <summary>
        /// <para>Get whether the outlet on the device can supply power to any connected electrical consumers or not.</para>
        /// <para>This is unrelated to whether the entire Kasa device is running. If you can connect to it, it's running.</para>
        /// </summary>
        /// <returns><c>true</c> if the outlet's internal relay is on, or <c>false</c> if it's off</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task<bool> IsOutletOn();

        /// <summary>
        /// <para>Turn on or off the device's outlet so it can supply power to any connected electrical consumers or not.</para>
        /// <para>You can also toggle the outlet by pressing the physical button on the device.</para>
        /// <para>This call is idempotent: if you try to turn the outlet on and it's already on, the call will have no effect.</para>
        /// <para>The state is persisted across restarts. If the device loses power, it will restore the previous outlet power state when it turns on again.</para>
        /// <para>This call is unrelated to turning the entire Kasa device on or off. To reboot the device, use <see cref="Reboot"/>.</para>
        /// </summary>
        /// <param name="turnOn"><c>true</c> to supply power to the outlet, or <c>false</c> to switch if off.</param>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task SetOutletOn(bool turnOn);

        /// <summary>
        /// <para>Get data about the device, including hardware, software, configuration, and current state.</para>
        /// </summary>
        /// <returns>Data about the device</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task<SystemInfo> GetInfo();

        /// <summary>
        /// <para>Outlets have a physical status light that shows whether they are supplying power to consumers or not.</para>
        /// <para>This light can be disabled even when the outlet is on, for example if it's annoyingly bright in a room where you're trying to watch a movie or go to sleep.</para>
        /// </summary>
        /// <returns><c>true</c> if the light will turn on whenever the outlet is supplying power, or <c>false</c> if the light will stay off regardless of whether or not the outlet is supplying power</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task<bool> IsIndicatorLightOn();

        /// <summary>
        /// <para>Outlets have a physical status light that shows whether they are supplying power to consumers or not.</para>
        /// <para>This light can be disabled even when the outlet is on, for example if it's annoyingly bright in a room where you're trying to watch a movie or go to sleep.</para>
        /// </summary>
        /// <param name="turnOn"><c>true</c> if you want the light to turn on whenever the outlet is supplying power, or <c>false</c> if you want the light to stay off regardless of whether or not the outlet is supplying power</param>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task SetIndicatorLightOn(bool turnOn);

        /// <summary>
        /// <para>Restart the device.</para>
        /// <para>Rebooting will interrupt power to any connected consumers for roughly 108 milliseconds.</para>
        /// <para>It takes about 8 seconds for a KP125 to completely reboot and resume responding to API requests, and about 14 seconds for an EP10.</para>
        /// <para>The existing outlet power state will be retained after rebooting, so if it was on before rebooting, it will turn on again after rebooting, and there is no need to explicitly call <see cref="SetOutletOn"/> to reestablish the previous state.</para>
        /// <para>By default, this client will automatically reconnect to the outlet after it reboots, which can be tuned using the <see cref="IOptions.MaxAttempts"/> and <see cref="IOptions.RetryDelay"/> properties.</para>
        /// </summary>
        /// <param name="afterDelay">How long to wait before rebooting. If not specified, the device reboots immediately.</param>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task Reboot(TimeSpan afterDelay = default);

        /// <summary>
        /// <para>Change the alias of this device. This will appear in the Kasa mobile app.</para>
        /// </summary>
        /// <param name="name">The new name of the device. The maximum length is 31 characters.</param>
        /// <exception cref="ArgumentOutOfRangeException">if the new name is empty or longer than 31 characters</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task SetName(string name);

        // Task SetLocation(double latitude, double longitude);

    }

    /// <summary>
    /// Commands that deal with the device's internal clock that keeps track of the current date and time. This is unrelated to schedules and timers that control when the outlet turns on or off.
    /// </summary>
    public interface ITimeCommands {

        /// <summary>
        /// <para>Get the current time from the device's internal clock.</para>
        /// </summary>
        /// <returns>The date and time of the device, in the device's current timezone.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task<DateTime> GetTime();

        /// <summary>
        /// <para>Get the current time and time zone from the device's internal clock.</para>
        /// </summary>
        /// <returns>The date, time, and time zone offset of the device, in the device's current timezone.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task<DateTimeOffset> GetTimeWithZoneOffset();

        /// <summary>
        /// <para>Get a list of possible time zones that the device is in.</para>
        /// <para>This may return multiple possibilities instead of one time zone because, unfortunately, Kasa devices internally represent multiple time zones with non-unique identifiers.</para>
        /// <para>For example, <c>Central Standard Time</c> is unambiguously stored as <c>13</c> on the Kasa device, so this method will only return that time zone.</para>
        /// <para>However, <c>Eastern Standard Time</c> is stored as <c>18</c> on the Kasa device, which collides with <c>18</c> that it also uses to represent <c>Eastern Standard Time (Mexico)</c>, <c>Turks and Caicos Standard Time</c>, <c>Haiti Standard Time</c>, and <c>Easter Island Standard Time</c>, so this method will return all five possibilities since they cannot be distinguished based on the information provided by the device.</para>
        /// </summary>
        /// <returns>A enumerable of possible time zones for which the device may be configured. It will never be empty or null.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task<IEnumerable<TimeZoneInfo>> GetTimeZones();

        /// <summary>
        /// <para>Configure the device to use a specific time zone.</para>
        /// </summary>
        /// <param name="timeZone">The time zone that you want the device to use with its internal clock.</param>
        /// <exception cref="TimeZoneNotFoundException">If the time zone you specified doesn't exist on Kasa devices. As of 2022-06-01, the only two known examples are <c>Magallanes Standard Time (America/Punta_Arenas)</c> and the made-up <c>Mid-Atlantic Standard Time.</c></exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task SetTimeZone(TimeZoneInfo timeZone);

    }

    /// <summary>
    /// <para>Commands that deal with the energy meter present in some Kasa devices, such as the KP125 and KP115.</para>
    /// <para>To determine if your device has an energy meter, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.EnergyMeter)</c>.</para>
    /// </summary>
    public interface IEnergyMeterCommands {

        /// <summary>
        /// Fetch a point-in-time measurement of the instantaneous electrical usage of the outlet.
        /// </summary>
        /// <returns>The milliamps, millivolts, and milliwatts being used by this outlet right now, as well as total watt-hours used since boot.</returns>
        /// <exception cref="FeatureUnavailable">If the device does not have an energy meter. To check this, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.EnergyMeter)</c>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task<PowerUsage> GetInstantaneousPowerUsage();

        /// <summary>
        /// Fetch a historical report of cumulative energy usage, grouped by day, from a given month and year.
        /// </summary>
        /// <param name="year">the year to fetch historical data for, e.g. <c>2022</c></param>
        /// <param name="month">the month to fetch historical data for, where January is <c>1</c></param>
        /// <returns>An array of integers, where the index is the day of the given month where the first day of the month has index <c>0</c>, and the value is the amount of energy used on that day, in watt-hours (W⋅h). If no historical data exists for that month, returns <c>null</c>.</returns>
        /// <exception cref="FeatureUnavailable">If the device does not have an energy meter. To check this, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.EnergyMeter)</c>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task<IList<int>?> GetDailyEnergyUsage(int year, int month);

        /// <summary>
        /// Fetch a historical report of cumulative energy usage, grouped by month, from a given year.
        /// </summary>
        /// <param name="year">the year to fetch historical data for, e.g. <c>2022</c></param>
        /// <returns>An array of integers, where the index is the month of the given year where January has index <c>0</c>, and the value is the amount of energy used in that month, in watt-hours (W⋅h). If no historical data exists for that year, returns <c>null</c>.</returns>
        /// <exception cref="FeatureUnavailable">If the device does not have an energy meter. To check this, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.EnergyMeter)</c>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task<IList<int>?> GetMonthlyEnergyUsage(int year);

        /// <summary>
        /// <para>Clear all energy usage data for all days, months, and years, and begin gathering new data from a fresh start.</para>
        /// <para>After calling this method, subsequent calls to <see cref="GetDailyEnergyUsage"/> and <see cref="GetMonthlyEnergyUsage"/> will return <c>null</c> for past months and years, respectively. The current month and year's data will be reset to <c>0</c>, respectively. In addition, subsequent calls to <see cref="GetInstantaneousPowerUsage"/> will return <c>0</c> for <see cref="PowerUsage.CumulativeEnergySinceBoot"/>, although it will not affect the point-in-time, non-historical measurements <see cref="PowerUsage.Current"/>, <see cref="PowerUsage.Voltage"/>, and <see cref="PowerUsage.Power"/>.</para>
        /// </summary>
        /// <exception cref="FeatureUnavailable">If the device does not have an energy meter. To check this, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.EnergyMeter)</c>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
        Task DeleteHistoricalUsage();

    }

}