using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Kasa;

public interface IKasaSmartOutlet: IDisposable {

    string Hostname { get; }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SocketException"></exception>
    Task Connect();

    ISystemCommands System { get; }
    ITimeCommands Time { get; }

    public interface ISystemCommands {

        Task<bool> IsOutletOn();
        Task SetOutlet(bool turnOn);

        Task<SystemInfo> GetSystemInfo();

        Task<bool> IsIndicatorLightOn();
        Task SetIndicatorLight(bool turnOn);

        Task Reboot(TimeSpan afterDelay);

        Task SetName(string name);

        // Task SetLocation(double latitude, double longitude);

    }

    public interface ITimeCommands {

        Task<DateTime> GetTime();
        // Task<int> GetTimezone();
        // Task SetTimezone();

    }

}