namespace Kasa;

internal interface IKasaClient: IDisposable {

    string Hostname { get; }
    Options Options { get; set; }

    bool Connected { get; }

    /// <exception cref="ObjectDisposedException">This instance has already been disposed.</exception>
    /// <exception cref="NetworkException">The TCP connection failed.</exception>
    Task Connect();

    /// <exception cref="FeatureUnavailable">If the device is missing a feature that is required to run the given method, such as running <c>EnergyMeter.GetInstantaneousPowerUsage()</c> on an EP10, which does not have the EnergyMeter Feature.</exception>
    /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
    /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
    Task<T> Send<T>(CommandFamily commandFamily, string methodName, object? parameters = null);

}