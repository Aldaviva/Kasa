using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kasa;

/// <summary>
/// <para>A TP-Link Kasa outlet or plug. This interface is the main entry point of this library. The corresponding implementation is <see cref="KasaOutlet"/>.</para>
/// <para>You must call <see cref="Connect"/> on each instance before using it.</para>
/// <para>Usage:</para>
/// <code>using IKasaOutlet outlet = new KasaOutlet("192.168.1.100");
/// await outlet.Connect();
/// bool isOutletOn = await outlet.System.IsOutletOn();
/// if(!isOutletOn){
///     await outlet.System.SetOutlet(true);
/// }</code>
/// </summary>
public interface IKasaOutlet: IDisposable {

    /// <summary>
    /// The hostname that you specified for the client when you constructed the <see cref="KasaOutlet"/>. Can be an IP address or FQDN.
    /// </summary>
    string Hostname { get; }

    /// <summary>
    /// Required before calling any commands on the outlet. Connects to the outlet using the given hostname.
    /// </summary>
    /// <exception cref="InvalidOperationException">This instance is already connected or has already been disposed.</exception>
    /// <exception cref="SocketException">The TCP connection failed.</exception>
    Task Connect();

    /// <summary>
    /// Commands that get or set system properties, like status, name, and whether the outlet is on or off.
    /// </summary>
    ISystemCommands System { get; }

    /// <summary>
    /// Commands that deal with the outlet's internal clock that keeps track of the current date and time. This is unrelated to schedules and timers that control when the outlet turns or off.
    /// </summary>
    ITimeCommands Time { get; }

    /// <summary>
    /// Commands that get or set system properties, like status, name, and whether the outlet is on or off.
    /// </summary>
    public interface ISystemCommands {

        /// <summary>
        /// <para>Get whether the outlet on the device can supply power to any connected electrical consumers or not.</para>
        /// <para>This is unrelated to whether the entire Kasa device is running. If you can connect to it, it's running.</para>
        /// </summary>
        /// <returns><c>true</c> if the outlet's internal relay is on, or <c>false</c> if it's off</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SocketException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="JsonReaderException"></exception>
        Task<bool> IsOutletOn();

        /// <summary>
        /// <para>Turn on or off the device's outlet so it can supply power to any connected electrical consumers or not.</para>
        /// <para>You can also toggle the outlet by pressing the physical button on the device.</para>
        /// <para>This call is idempotent: if you try to turn the outlet on and it's already on, the call will have no effect.</para>
        /// <para>The state is persisted across restarts. If the device loses power, it will restore the previous outlet power state when it turns on again.</para>
        /// <para>This call is unrelated to turning the entire Kasa device on or off. To reboot the device, use <see cref="Reboot"/>.</para>
        /// </summary>
        /// <param name="turnOn"><c>true</c> to supply power to the outlet, or <c>false</c> to switch if off.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SocketException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="JsonReaderException"></exception>
        Task SetOutlet(bool turnOn);

        /// <summary>
        /// <para>Get data about the device, including hardware, software, configuration, and current state.</para>
        /// </summary>
        /// <returns>Data about the device</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SocketException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="JsonReaderException"></exception>
        Task<SystemInfo> GetSystemInfo();

        /// <summary>
        /// <para>Outlets have a physical status light that shows whether they are supplying power to consumers or not.</para>
        /// <para>This light can be disabled even when the outlet is on, for example if it's annoyingly bright in a room where you're trying to watch a movie or go to sleep.</para>
        /// </summary>
        /// <returns><c>true</c> if the light will turn on whenever the outlet is supplying power, or <c>false</c> if the light will stay off regardless of whether or not the outlet is supplying power</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SocketException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="JsonReaderException"></exception>
        Task<bool> IsIndicatorLightOn();

        /// <summary>
        /// <para>Outlets have a physical status light that shows whether they are supplying power to consumers or not.</para>
        /// <para>This light can be disabled even when the outlet is on, for example if it's annoyingly bright in a room where you're trying to watch a movie or go to sleep.</para>
        /// </summary>
        /// <param name="turnOn"><c>true</c> if you want the light to turn on whenever the outlet is supplying power, or <c>false</c> if you want the light to stay off regardless of whether or not the outlet is supplying power</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SocketException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="JsonReaderException"></exception>
        Task SetIndicatorLight(bool turnOn);

        /// <summary>
        /// <para>Restart the device.</para>
        /// </summary>
        /// <param name="afterDelay">How long to wait before rebooting. If not specified, the device reboots immediately.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SocketException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="JsonReaderException"></exception>
        // TODO figure out if this interrupts power to consumers
        // TODO figure out if you need to reconnect (or make a new KasaOutlet) after calling this method
        Task Reboot(TimeSpan afterDelay = default);

        /// <summary>
        /// <para>Change the alias of this device. This will appear in the Kasa mobile app.</para>
        /// </summary>
        /// <param name="name">The new name of the device. The maximum length is 31 characters.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SocketException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="JsonReaderException"></exception>
        /// <exception cref="ArgumentOutOfRangeException">if the new name is empty or longer than 31 characters</exception>
        Task SetName(string name);

        // Task SetLocation(double latitude, double longitude);

    }

    /// <summary>
    /// Commands that deal with the outlet's internal clock that keeps track of the current date and time. This is unrelated to schedules and timers that control when the outlet turns or off.
    /// </summary>
    public interface ITimeCommands {

        /// <summary>
        /// <para>Get the current time from the device's internal clock.</para>
        /// </summary>
        /// <returns>The date and time of the device, in its current timezone.</returns>
        /// <exception cref="SocketException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="JsonReaderException"></exception>
        Task<DateTime> GetTime();

        Task<TimeZoneInfo> GetTimeZone();

        Task SetTimezone(TimeZoneInfo newZone);

    }

}