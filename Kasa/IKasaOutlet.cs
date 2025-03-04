using System.ComponentModel;

namespace Kasa;

/// <summary>
/// <para>A TP-Link Kasa outlet. This is a super-interface for the main entry points to this library. To create an instance, construct a new <see cref="KasaOutlet"/> if your device has exactly one socket (like an EP10), or a new <see cref="MultiSocketKasaOutlet"/> if your device has multiple sockets (like an EP40).</para>
/// <para>You may optionally call <see cref="Connect"/> on each instance before using it. If you don't, it will connect automatically when sending the first command.</para>
/// <para>Remember to <c>Dispose</c> each instance when you're done using it in order to close the TCP connection with the device. Disposed instances may not be reused, even if you call <see cref="Connect"/> again.</para>
/// <para>To communicate with multiple Kasa devices, construct multiple <see cref="KasaOutlet"/> instances, one per device.</para>
/// <para>Example usage:</para>
/// <code>using IKasaOutlet outlet = new KasaOutlet("192.168.1.100");
/// bool isSocketOn = await outlet.System.IsSocketOn();
/// if (!isSocketOn) {
///     await outlet.System.SetSocketOn(true);
/// }</code>
/// </summary>
public interface IKasaOutletBase: IDisposable {

    /// <summary>
    /// The hostname that you specified for the client when you constructed the <see cref="KasaOutlet"/>. Can be an IP address or FQDN.
    /// </summary>
    string Hostname { get; }

    /// <summary>
    /// Non-required configuration parameters for the <see cref="IKasaOutlet"/>, which can be used to fine-tune its behavior.
    /// </summary>
    Options Options { get; set; }

    /// <summary>
    /// <para>Eagerly connects to the outlet using the given hostname.</para>
    /// <para>You may optionally call this to explicitly connect before sending any commands on the outlet.</para>
    /// <para>If you don't call this method before sending a command (such as <c>IKasaOutlet.System.GetInfo()</c>), then this instance will automatically lazily connect before sending that command.</para>
    /// <para>Explicit connection may be more helpful for early detection of errors, as well as for reducing the latency of the first command. Automatic connection may be more convenient because there are fewer methods to invoke.</para>
    /// <para>If this instance is already connected, then this call does nothing and returns immediately.</para>
    /// </summary>
    /// <exception cref="ObjectDisposedException">This instance has already been disposed.</exception>
    /// <exception cref="NetworkException">The TCP connection failed.</exception>
    Task Connect();

    /// <summary>
    /// <para>Commands that deal with the outlet's internal clock that keeps track of the current date and time.</para>
    /// <para>This is unrelated to schedules and timers that control when the outlet turns or off, see <see cref="Timer"/>.</para>
    /// </summary>
    ITimeCommands Time { get; }

    /// <summary>
    /// <para>Commands that deal with the energy consumption of the electrical consumers attached to the outlet.</para>
    /// <para>These commands are not available on all Kasa devices – they require the <see cref="Feature.EnergyMeter"/> feature, which is only available on models like KP125, KP115, HS300, and HS110.</para>
    /// <para>To check if your device has this feature and can handle these commands, you can call <c>(await IKasaOutlet.System.GetInfo()).Features.Contains(Features.EnergyMeter)</c>.</para>
    /// </summary>
    IEnergyMeterCommands EnergyMeter { get; }

    /// <summary>
    /// <para>Commands that deal with the connection between the device and the TP-Link hosted servers.</para>
    /// <para>This is referred to as Remote Control in the Kasa Smart phone app. It allows the outlet to be controlled by the phone app without your phone having to be on the same local network as the outlet.</para>
    /// </summary>
    ICloudCommands Cloud { get; }

    /// <summary>
    /// Commands that get or set system properties, like status, name, and whether the outlet is on or off.
    /// </summary>
    public interface ISystemCommands {

        /// <summary>
        /// <para>Get data about the device, including hardware, software, configuration, and current state.</para>
        /// </summary>
        /// <returns>Data about the device</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<SystemInfo> GetInfo();

        /// <summary>
        /// <para>Outlets have a physical status light that shows whether they are supplying power to consumers or not.</para>
        /// <para>This light can be disabled even when the outlet is on, for example if it's annoyingly bright in a room where you're trying to watch a movie or go to sleep.</para>
        /// </summary>
        /// <returns><c>true</c> if the light will turn on whenever the outlet is supplying power, or <c>false</c> if the light will stay off regardless of whether or not the outlet is supplying power</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<bool> IsIndicatorLightOn();

        /// <summary>
        /// <para>Outlets have a physical status light that shows whether they are supplying power to consumers or not.</para>
        /// <para>This light can be disabled even when the outlet is on, for example if it's annoyingly bright in a room where you're trying to watch a movie or go to sleep.</para>
        /// </summary>
        /// <param name="turnOn"><c>true</c> if you want the light to turn on whenever the outlet is supplying power, or <c>false</c> if you want the light to stay off regardless of whether or not the outlet is supplying power</param>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task SetIndicatorLightOn(bool turnOn);

        /// <summary>
        /// <para>Restart the device.</para>
        /// <para>Rebooting will interrupt power to any connected consumers for roughly 108 milliseconds.</para>
        /// <para>It takes about 8 seconds for a KP125 to completely reboot and resume responding to API requests, and about 14 seconds for an EP10.</para>
        /// <para>The existing outlet power state will be retained after rebooting, so if it was on before rebooting, it will turn on again after rebooting, and there is no need to explicitly call <see cref="ISingleSocket.SetSocketOn"/> to reestablish the previous state.</para>
        /// <para>By default, this client will automatically reconnect to the outlet after it reboots, which can be tuned using the <see cref="Options.MaxAttempts"/> and <see cref="Options.RetryDelay"/> properties.</para>
        /// </summary>
        /// <param name="afterDelay">How long to wait before rebooting. If not specified, the device reboots immediately.</param>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task Reboot(TimeSpan afterDelay = default);

        /// <summary>
        /// <para>The name or alias of the device that you chose during setup.</para>
        /// </summary>
        /// <returns>The name of the device.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<string> GetName();

        /// <summary>
        /// <para>Change the alias of this device. This will appear in the Kasa mobile app.</para>
        /// </summary>
        /// <param name="name">The new name of the device. The maximum length is 31 characters.</param>
        /// <exception cref="ArgumentOutOfRangeException">if the new name is empty or longer than 31 characters</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task SetName(string name);

        /// <summary>
        /// <para>Count how many physical AC sockets this Kasa device has. This is not related to TCP/IP socket connections.</para>
        /// <para>This only includes the main three-prong 15A AC NEMA 5-15-R sockets. It excludes all USB-A ports, such as those that appear on the KP303 or HS300 power strips, which are not individually switched.</para>
        /// </summary>
        /// <returns>The number of AC sockets on this device, determined by actually querying the device, rather than the class. This means that if you construct an instance of single socket <see cref="KasaOutlet"/> with the hostname of a dual socket EP40 outlet, this will return 2 instead of 1, which indicates that you may want to dispose the <see cref="KasaOutlet"/> and construct a <see cref="MultiSocketKasaOutlet"/> to use with the EP40 instead.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<int> CountSockets();

        /// <summary>
        /// Commands that get or set system properties, like status, name, and whether the socket is on or off. Includes commands that apply to devices with exactly one socket.
        /// </summary>
        public interface ISingleSocket: ISystemCommands {

            /// <summary>
            /// <para>Get whether the socket on the device is energized.</para>
            /// <para>This is unrelated to whether the entire Kasa device is running. If you can connect to it, it's running.</para>
            /// </summary>
            /// <returns><c>true</c> if the socket's internal relay is on, or <c>false</c> if it's off</returns>
            /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
            /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
            Task<bool> IsSocketOn();

            /// <summary>
            /// <para>Energizes or deenergizes the device's socket so it can supply power to any connected electrical consumers or not.</para>
            /// <para>You can also toggle the socket by pressing the physical button on the device.</para>
            /// <para>This call is idempotent: if you try to turn the socket on and it's already on, the call will have no effect.</para>
            /// <para>The state is persisted across restarts. If the device loses power, it will restore the previous socket power state when it turns on again.</para>
            /// <para>This call is unrelated to turning the entire Kasa device on or off. To reboot the device, use <see cref="ISystemCommands.Reboot"/>.</para>
            /// </summary>
            /// <param name="turnOn"><c>true</c> to supply power to the socket, or <c>false</c> to switch it off.</param>
            /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
            /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
            Task SetSocketOn(bool turnOn);

            /// <inheritdoc cref="IsSocketOn" />
            [Obsolete($"This method was poorly named, and has been renamed to {nameof(IsSocketOn)}", false)]
            [EditorBrowsable(EditorBrowsableState.Never)]
            Task<bool> IsOutletOn();

            /// <inheritdoc cref="SetSocketOn" />
            [Obsolete($"This method was poorly named, and has been renamed to {nameof(SetSocketOn)}", false)]
            [EditorBrowsable(EditorBrowsableState.Never)]
            Task SetOutletOn(bool turnOn);

        }

        /// <summary>
        /// Commands that get or set system properties, like status, name, and whether a socket is on or off. Includes commands that apply to devices with more than one socket.
        /// </summary>
        public interface IMultiSocket: ISystemCommands {

            /// <summary>
            /// <para>Get whether a socket on the device is energized.</para>
            /// <para>This is unrelated to whether the entire Kasa device is running. If you can connect to it, it's running.</para>
            /// </summary>
            /// <param name="socketId">The index, increasing from 0, of the outlet on the device. For example, to query the socket with the physical label "plug 1," pass 0 to this parameter.</param>
            /// <returns><c>true</c> if the socket's internal relay is on, or <c>false</c> if it's off</returns>
            /// <exception cref="ArgumentOutOfRangeException">if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
            /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
            /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
            Task<bool> IsSocketOn(int socketId);

            /// <summary>
            /// <para>Energizes or deenergizes one of the device's sockets so it can supply power to a connected electrical consumer or not.</para>
            /// <para>You can also toggle the socket by pressing a physical button on the device.</para>
            /// <para>This call is idempotent: if you try to turn the socket on and it's already on, the call will have no effect.</para>
            /// <para>The state is persisted across restarts. If the device loses power, it will restore the previous socket power states when it turns on again.</para>
            /// <para>This call is unrelated to turning the entire Kasa device on or off. To reboot the device, use <see cref="ISystemCommands.Reboot"/>.</para>
            /// </summary>
            /// <param name="socketId">The index, increasing from 0, of the socket on the device. For example, to control the socket with the physical label "plug 1," pass 0 to this parameter.</param>
            /// <param name="turnOn"><c>true</c> to supply power to the socket, or <c>false</c> to switch it off.</param>
            /// <exception cref="ArgumentOutOfRangeException">if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
            /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
            /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
            Task SetSocketOn(int socketId, bool turnOn);

            /// <summary>
            /// <para>The name or alias of an individual socket that you chose during setup.</para>
            /// <para>To get the alias of the entire device, see <see cref="ISystemCommands.GetName"/>.</para>
            /// </summary>
            /// <param name="socketId">The index, increasing from 0, of the socket on the device. For example, to specify the socket with the physical label "plug 1," pass 0 to this parameter.</param>
            /// <returns>The name of the individual socket.</returns>
            /// <exception cref="ArgumentOutOfRangeException">if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
            /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
            /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
            Task<string> GetName(int socketId);

            /// <summary>
            /// <para>Change the alias of an individual socket. This will appear in the Kasa mobile app.</para>
            /// <para>To change the alias of the entire device, see <see cref="ISystemCommands.SetName"/>.</para>
            /// </summary>
            /// <param name="socketId">The index, increasing from 0, of the socket on the device. For example, to specify the socket with the physical label "plug 1," pass 0 to this parameter.</param>
            /// <param name="name">The new name of the device. The maximum length is 31 characters.</param>
            /// <exception cref="ArgumentOutOfRangeException">if the new name is empty or longer than 31 characters; or if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
            /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
            /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
            Task SetName(int socketId, string name);

        }

    }

    /// <summary>
    /// Commands that deal with the device's internal clock that keeps track of the current date and time. This is unrelated to schedules and timers that control when the outlet turns on or off.
    /// </summary>
    public interface ITimeCommands {

        /// <summary>
        /// <para>Get the current local time from the device's internal clock.</para>
        /// </summary>
        /// <returns>The date and time of the device, in the device's current timezone.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<DateTime> GetTime();

        /// <summary>
        /// <para>Get the current time and time zone from the device's internal clock.</para>
        /// </summary>
        /// <returns>The date, time, and time zone offset of the device, in the device's current timezone.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<DateTimeOffset> GetTimeWithZoneOffset();

        /// <summary>
        /// <para>Get a list of possible time zones that the device is in.</para>
        /// <para>This may return multiple possibilities instead of one time zone because, unfortunately, Kasa devices internally represent multiple time zones with non-unique identifiers.</para>
        /// <para>For example, <c>Central Standard Time</c> is unambiguously stored as <c>13</c> on the Kasa device, so this method will only return that time zone.</para>
        /// <para>However, <c>Eastern Standard Time</c> is stored as <c>18</c> on the Kasa device, which collides with <c>18</c> that it also uses to represent <c>Eastern Standard Time (Mexico)</c>, <c>Turks and Caicos Standard Time</c>, <c>Haiti Standard Time</c>, and <c>Easter Island Standard Time</c>, so this method will return all five possibilities since they cannot be distinguished based on the information provided by the device.</para>
        /// </summary>
        /// <returns>A enumerable of possible time zones for which the device may be configured. It will never be empty or null.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<IEnumerable<TimeZoneInfo>> GetTimeZones();

        /// <summary>
        /// <para>Configure the device to use a specific time zone.</para>
        /// </summary>
        /// <param name="timeZone">The time zone that you want the device to use with its internal clock.</param>
        /// <exception cref="TimeZoneNotFoundException">If the time zone you specified doesn't exist on Kasa devices. As of 2022-06-01, the only two known examples are <c>Magallanes Standard Time (America/Punta_Arenas)</c> and the made-up <c>Mid-Atlantic Standard Time.</c></exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
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
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<PowerUsage> GetInstantaneousPowerUsage();

        /// <summary>
        /// Fetch a historical report of cumulative energy usage, grouped by day, from a given month and year.
        /// </summary>
        /// <param name="year">the year to fetch historical data for, e.g. <c>2022</c></param>
        /// <param name="month">the month to fetch historical data for, where January is <c>1</c></param>
        /// <returns>
        /// <para>An array of integers, in which the index is the day of the given month where the first day of the month has index <c>0</c>, and the value is the amount of energy used on that day in watt-hours (W⋅h).</para>
        /// <para>The length of the array will be the total number of days in the given month, even if some of the days have no usage data or are in the future, which will be represented with the value <c>0</c>.</para>
        /// <para>If no historical data exists for that month, returns <c>null</c>.</para>
        /// </returns>
        /// <exception cref="FeatureUnavailable">If the device does not have an energy meter. To check this, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.EnergyMeter)</c>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<IList<int>?> GetDailyEnergyUsage(int year, int month);

        /// <summary>
        /// Fetch a historical report of cumulative energy usage, grouped by month, from a given year.
        /// </summary>
        /// <param name="year">the year to fetch historical data for, e.g. <c>2022</c></param>
        /// <returns>An array of integers, where the index is the month of the given year where January has index <c>0</c>, and the value is the amount of energy used in that month, in watt-hours (W⋅h). If no historical data exists for that year, returns <c>null</c>.</returns>
        /// <exception cref="FeatureUnavailable">If the device does not have an energy meter. To check this, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.EnergyMeter)</c>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<IList<int>?> GetMonthlyEnergyUsage(int year);

        /// <summary>
        /// <para>Clear all energy usage data for all days, months, and years, and begin gathering new data from a fresh start.</para>
        /// <para>After calling this method, subsequent calls to <see cref="GetDailyEnergyUsage"/> and <see cref="GetMonthlyEnergyUsage"/> will return <c>null</c> for past months and years, respectively. The current month and year's data will be reset to <c>0</c>, respectively. In addition, subsequent calls to <see cref="GetInstantaneousPowerUsage"/> will return <c>0</c> for <see cref="PowerUsage.CumulativeEnergySinceBoot"/>, although it will not affect the point-in-time, non-historical measurements <see cref="PowerUsage.Current"/>, <see cref="PowerUsage.Voltage"/>, and <see cref="PowerUsage.Power"/>.</para>
        /// </summary>
        /// <exception cref="FeatureUnavailable">If the device does not have an energy meter. To check this, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.EnergyMeter)</c>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task DeleteHistoricalUsage();

    }

    /// <summary>
    /// <para>Countdown timers allow you to schedule the socket to turn on or off once after a delay of configurable duration.</para>
    /// <para>Outlets can handle at most one timer at once.</para>
    /// </summary>
    public interface ITimerCommandsSingleSocket {

        /// <summary>
        /// <para>Get the currently running countdown <see cref="Timer"/> rule on the device, along with its updated <see cref="Timer.RemainingDuration"/>.</para>
        /// <para>There can be either 0 or 1 timers on the device at once; multiple timers are not possible.</para>
        /// <para>If no timer has ever been created, it already elapsed, or you deleted it with <see cref="Clear"/>, this method will return <c>null</c>.</para>
        /// </summary>
        /// <returns>The current timer, which was running at the time of invocation, or <c>null</c> if there are no running timers on the device.</returns>
        /// <exception cref="FeatureUnavailable">If the device does not have a timer. To check this, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.Timer)</c>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<Timer?> Get();

        /// <summary>
        /// <para>Save a new, enabled countdown <see cref="Timer"/> to the device.</para>
        /// <para>There can be at most one timer on the device at once, so any existing timers will first be deleted, even if they had not elapsed yet.</para>
        /// <para>The created timer will be returned, which is useful if you want to inspect the newly-populated <see cref="Timer.RemainingDuration"/> property. To refresh <see cref="Timer.RemainingDuration"/> in the future, call <see cref="Get"/>.</para>
        /// </summary>
        /// <param name="duration">How long the timer should wait, since being started, before turning on or off.</param>
        /// <param name="setSocketOnWhenComplete">Whether to turn the socket on (<c>true</c>) or off (<c>false</c>) when the timer elapses.</param>
        /// <returns>The created timer rule, which will have the same <see cref="Timer.TotalDuration"/> and <see cref="Timer.WillSetSocketOn"/> as the <paramref name="duration"/> and <paramref name="setSocketOnWhenComplete"/> you passed in, but with a populated <see cref="Timer.RemainingDuration"/>.</returns>
        /// <exception cref="FeatureUnavailable">If the device does not have a timer. To check this, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.Timer)</c>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<Timer> Start(TimeSpan duration, bool setSocketOnWhenComplete);

        /// <summary>
        /// <para>Delete any existing timer rule from the device, cancelling its countdown.</para>
        /// <para>This will cause <see cref="Get"/> to return <c>null</c> until you <see cref="Start"/> a new <see cref="Timer"/>.</para>
        /// <para>Idempotent: this will succeed even if there are no timers to delete.</para>
        /// </summary>
        /// <exception cref="FeatureUnavailable">If the device does not have a timer. To check this, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.Timer)</c>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task Clear();

    }

    /// <inheritdoc cref="ITimerCommandsSingleSocket" />
    public interface ITimerCommandsMultiSocket {

        /// <inheritdoc cref="ITimerCommandsSingleSocket.Get" />
        /// <param name="socketId">The index, increasing from 0, of the socket on the device. For example, to specify the socket with the physical label "plug 1," pass 0 to this parameter.</param>
        /// <exception cref="ArgumentOutOfRangeException">if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
        Task<Timer?> Get(int socketId);

        /// <summary>
        /// <para>Save a new, enabled countdown <see cref="Timer"/> to the device.</para>
        /// <para>There can be at most one timer on the device at once, so any existing timers will first be deleted, even if they had not elapsed yet.</para>
        /// <para>The created timer will be returned, which is useful if you want to inspect the newly-populated <see cref="Timer.RemainingDuration"/> property. To refresh <see cref="Timer.RemainingDuration"/> in the future, call <see cref="Get"/>.</para>
        /// </summary>
        /// <param name="duration">How long the timer should wait, since being started, before turning on or off.</param>
        /// <param name="setSocketOnWhenComplete">Whether to turn the socket on (<c>true</c>) or off (<c>false</c>) when the timer elapses.</param>
        /// <param name="socketId">The index, increasing from 0, of the socket on the device. For example, to specify the socket with the physical label "plug 1," pass 0 to this parameter.</param>
        /// <returns>The created timer rule, which will have the same <see cref="Timer.TotalDuration"/> and <see cref="Timer.WillSetSocketOn"/> as the <paramref name="duration"/> and <paramref name="setSocketOnWhenComplete"/> you passed in, but with a populated <see cref="Timer.RemainingDuration"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
        /// <exception cref="FeatureUnavailable">If the device does not have a timer. To check this, you can call <c>(await kasaOutlet.System.GetInfo()).Features.Contains(Feature.Timer)</c>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<Timer> Start(int socketId, TimeSpan duration, bool setSocketOnWhenComplete);

        /// <inheritdoc cref="ITimerCommandsSingleSocket.Clear" />
        /// <param name="socketId">The index, increasing from 0, of the socket on the device. For example, to specify the socket with the physical label "plug 1," pass 0 to this parameter.</param>
        /// <exception cref="ArgumentOutOfRangeException">if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
        Task Clear(int socketId);

    }

    /// <summary>
    /// <para>Commands that deal with schedules.</para>
    /// <para>Schedules allow you to set the socket to turn on or off once on a specific date and time, or on multiple days with a weekly recurrence pattern. Times can be relative to the start of the day, sunrise, or sunset.</para>
    /// </summary>
    public interface IScheduleCommandsSingleSocket {

        /// <summary>
        /// Fetch all of the existing schedules from the socket.
        /// </summary>
        /// <returns>A enumerable of <see cref="Schedule"/> rules, or the empty enumerable if the device has no schedules on it.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<IEnumerable<Schedule>> GetAll();

        /// <summary>
        /// <para>Persist a schedule to the socket.</para>
        /// <para>To insert a new schedule, construct a new <see cref="Schedule"/> instance, leaving its <see cref="Schedule.Id"/> property <c>null</c>. After saving it with this method, the returned instance will be a copy with the <see cref="Schedule.Id"/> value populated.</para>
        /// <para>To update an existing schedule, retrieve it using <see cref="GetAll"/>, make any changes you like, then save it with this method.</para>
        /// </summary>
        /// <param name="schedule">A new or existing schedule to insert or update.</param>
        /// <returns>The persisted instance, which always has a non-null <see cref="Schedule.Id"/> value.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<Schedule> Save(Schedule schedule);

        /// <summary>
        /// Remove an existing schedule from the socket.
        /// </summary>
        /// <param name="schedule">The existing schedule to delete.</param>
        /// <returns>Returns successfully when a schedule with the given <see cref="Schedule.Id"/> doesn't exist on the socket, even if this method invocation didn't delete it.</returns>
        /// <exception cref="ArgumentException">If the <paramref name="schedule"/> parameter has a <c>null</c> value for the <see cref="Schedule.Id"/> property, possibly because it was newly constructed instead of being fetched from <see cref="GetAll"/> or <see cref="Save"/>.</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task Delete(Schedule schedule);

        /// <summary>
        /// Remove an existing schedule from the socket.
        /// </summary>
        /// <param name="scheduleId">The <see cref="Schedule.Id"/> of an existing schedule to delete.</param>
        /// <returns>Returns successfully when a schedule with the given <see cref="Schedule.Id"/> doesn't exist on the socket, even if this method invocation didn't delete it.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task Delete(string scheduleId);

        /// <summary>
        /// Clear all existing schedules from the socket.
        /// </summary>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task DeleteAll();

    }

    /// <inheritdoc cref="IScheduleCommandsSingleSocket" />
    public interface IScheduleCommandsMultiSocket {

        /// <summary>
        /// Fetch all of the existing schedules from the socket.
        /// </summary>
        /// <param name="socketId">The index, increasing from 0, of the socket on the device. For example, to specify the socket with the physical label "plug 1," pass 0 to this parameter.</param>
        /// <returns>A enumerable of <see cref="Schedule"/> rules, or the empty enumerable if the device has no schedules on it.</returns>
        /// <exception cref="ArgumentOutOfRangeException">if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<IEnumerable<Schedule>> GetAll(int socketId);

        /// <summary>
        /// <para>Persist a schedule to the socket.</para>
        /// <para>To insert a new schedule, construct a new <see cref="Schedule"/> instance, leaving its <see cref="Schedule.Id"/> property <c>null</c>. After saving it with this method, the returned instance will be a copy with the <see cref="Schedule.Id"/> value populated.</para>
        /// <para>To update an existing schedule, retrieve it using <see cref="GetAll"/>, make any changes you like, then save it with this method.</para>
        /// </summary>
        /// <param name="socketId">The index, increasing from 0, of the socket on the device. For example, to specify the socket with the physical label "plug 1," pass 0 to this parameter.</param>
        /// <param name="schedule">A new or existing schedule to insert or update.</param>
        /// <returns>The persisted instance, which always has a non-null <see cref="Schedule.Id"/> value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<Schedule> Save(int socketId, Schedule schedule);

        /// <summary>
        /// Remove an existing schedule from the socket.
        /// </summary>
        /// <param name="socketId">The index, increasing from 0, of the socket on the device. For example, to specify the socket with the physical label "plug 1," pass 0 to this parameter.</param>
        /// <param name="schedule">The existing schedule to delete.</param>
        /// <returns>Returns successfully when a schedule with the given <see cref="Schedule.Id"/> doesn't exist on the socket, even if this method invocation didn't delete it.</returns>
        /// <exception cref="ArgumentException">if the <paramref name="schedule"/> parameter has a <c>null</c> value for the <see cref="Schedule.Id"/> property, possibly because it was newly constructed instead of being fetched from <see cref="GetAll"/> or <see cref="Save"/>; or if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task Delete(int socketId, Schedule schedule);

        /// <summary>
        /// Remove an existing schedule from the socket.
        /// </summary>
        /// <param name="socketId">The index, increasing from 0, of the socket on the device. For example, to specify the socket with the physical label "plug 1," pass 0 to this parameter.</param>
        /// <param name="scheduleId">The <see cref="Schedule.Id"/> of an existing schedule to delete.</param>
        /// <returns>Returns successfully when a schedule with the given <see cref="Schedule.Id"/> doesn't exist on the socket, even if this method invocation didn't delete it.</returns>
        /// <exception cref="ArgumentOutOfRangeException">if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task Delete(int socketId, string scheduleId);

        /// <summary>
        /// Clear all existing schedules from the socket.
        /// </summary>
        /// <param name="socketId">The index, increasing from 0, of the socket on the device. For example, to specify the socket with the physical label "plug 1," pass 0 to this parameter.</param>
        /// <exception cref="ArgumentOutOfRangeException">if <paramref name="socketId"/> is less than 0 or greater than or equal to the number of sockets on this device</exception>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task DeleteAll(int socketId);

    }

    /// <summary>
    /// <para>Commands that deal with the connection between the device and the TP-Link hosted servers.</para>
    /// <para>This is referred to as Remote Control in the Kasa Smart phone app. It allows the outlet to be controlled by the phone app without your phone having to be on the same local network as the outlet.</para>
    /// </summary>
    public interface ICloudCommands {

        /// <summary>
        /// Gets the status of the connection between the outlet and the TP-Link hosted servers.
        /// </summary>
        /// <returns><c>true</c> if the smart outlet has been provisioned with an account on the TP-Link hosted servers and can be controlled remotely over the Internet, or <c>false</c> if it is operating standalone and can only be controlled from the LAN.</returns>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task<bool> IsConnectedToCloudAccount();

        /// <summary>
        /// <para>Logs the smart outlet out of the TP-Link hosted servers, disconnecting it so it can't be remotely controlled over the Internet, and making it so it can only be controlled on the LAN. May make the device disappear from your Kasa Smart mobile app as well.</para>
        /// <para>If the device was already not logged into the TP-Link servers, the <see cref="Task"/> returned by this method will still resolve successfully — it is idempotent.</para>
        /// </summary>
        /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
        /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
        Task DisconnectFromCloudAccount();

    }

}

/// <summary>
/// <para>A TP-Link Kasa outlet. This interface is the main entry point to this library for devices with exactly one socket, like the EP10. For devices like the EP40 with multiple sockets, see <see cref="IMultiSocketKasaOutlet"/> instead. To create an instance, construct a new <see cref="KasaOutlet"/>.</para>
/// <para>You may optionally call <see cref="IKasaOutletBase.Connect"/> on each instance before using it. If you don't, it will connect automatically when sending the first command.</para>
/// <para>Remember to <c>Dispose</c> each instance when you're done using it in order to close the TCP connection with the device. Disposed instances may not be reused, even if you call <see cref="IKasaOutletBase.Connect"/> again.</para>
/// <para>To communicate with multiple Kasa devices, construct multiple <see cref="KasaOutlet"/> instances, one per device.</para>
/// <para>Example usage:</para>
/// <code>using IKasaOutlet outlet = new KasaOutlet("192.168.1.100");
/// bool isSocketOn = await outlet.System.IsSocketOn();
/// if (!isSocketOn) {
///     await outlet.System.SetSocketOn(true);
/// }</code>
/// </summary>
public interface IKasaOutlet: IKasaOutletBase {

    /// <summary>
    /// Commands that get or set system properties, like status, name, and whether the outlet is on or off.
    /// </summary>
    ISystemCommands.ISingleSocket System { get; }

    /// <summary>
    /// <para>Commands that deal with countdown timers.</para>
    /// <para>Timers allow you to set the outlet to turn on or off once after a delay of configurable duration.</para>
    /// <para>Outlets can handle at most one timer at once.</para>
    /// <para>This is unrelated to the current time of the device's internal clock, see <see cref="IKasaOutletBase.Time"/>.</para>
    /// </summary>
    ITimerCommandsSingleSocket Timer { get; }

    /// <summary>
    /// <para>Commands that deal with schedules.</para>
    /// <para>Schedules allow you to set the outlet to turn on or off once on a specific day and time, or multiple times with a weekly recurrence pattern. Times can be relative to the start of the day, sunrise, or sunset.</para>
    /// <para>Outlets can handle multiple schedules at once.</para>
    /// </summary>
    IScheduleCommandsSingleSocket Schedule { get; }

}

/// <summary>
/// <para>A TP-Link Kasa outlet with multiple sockets. This interface is the main entry point to this library for devices with more than one socket, like the EP40. For devices like the EP10 with exactly one socket, see <see cref="IKasaOutlet"/> instead. To create an instance, construct a new <see cref="MultiSocketKasaOutlet"/>. Individual socket IDs used in method parameters are 0-indexed.</para>
/// <para>You may optionally call <see cref="IKasaOutletBase.Connect"/> on each instance before using it. If you don't, it will connect automatically when sending the first command.</para>
/// <para>Remember to <c>Dispose</c> each instance when you're done using it in order to close the TCP connection with the device. Disposed instances may not be reused, even if you call <see cref="IKasaOutletBase.Connect"/> again.</para>
/// <para>To communicate with multiple Kasa devices, construct multiple <see cref="MultiSocketKasaOutlet"/> instances, one per device.</para>
/// <para>Example usage:</para>
/// <code>using IMultiSocketKasaOutlet outlet = new MultiSocketKasaOutlet("192.168.1.100");
/// bool isSocketOn = await outlet.System.IsSocketOn(0);
/// if (!isSocketOn) {
///     await outlet.System.SetSocketOn(0, true);
/// }</code>
/// </summary>
public interface IMultiSocketKasaOutlet: IKasaOutletBase {

    /// <inheritdoc cref="IKasaOutlet.System" />
    ISystemCommands.IMultiSocket System { get; }

    /// <inheritdoc cref="IKasaOutlet.Timer" />
    ITimerCommandsMultiSocket Timer { get; }

    /// <inheritdoc cref="IKasaOutlet.Schedule" />
    IScheduleCommandsMultiSocket Schedule { get; }

}