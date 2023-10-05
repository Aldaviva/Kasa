using System.Net.Sockets;

namespace Kasa;

/// <summary>
/// Base class for custom exceptions thrown by the <c>Kasa</c> library.
/// </summary>
public abstract class KasaException: Exception {

    /// <summary>
    /// The FQDN or IP address of the outlet, set when constructing the <see cref="KasaOutlet"/>.
    /// </summary>
    public string Hostname { get; }

    /// <summary>
    /// Base class for custom exceptions thrown by the <c>Kasa</c> library.
    /// </summary>
    protected KasaException(string message, string hostname, Exception? innerException = null): base(message, innerException) {
        Hostname = hostname;
    }

}

/// <summary>
/// <para>Exception thrown when the Kasa library encounters an unrecoverable TCP error while sending or receiving data to an outlet.</para>
/// <para>The <see cref="Exception.InnerException"/> is either an <see cref="IOException"/> or <see cref="SocketException"/>.</para>
/// </summary>
public class NetworkException: KasaException {

    /// <summary>
    /// <para>Exception thrown when the Kasa library encounters an unrecoverable TCP error while sending or receiving data to an outlet.</para>
    /// </summary>
    public NetworkException(string message, string hostname, IOException innerException): base(message, hostname, innerException) { }

    /// <summary>
    /// <para>Exception thrown when the Kasa library encounters an unrecoverable TCP error while sending or receiving data to an outlet.</para>
    /// </summary>
    public NetworkException(string message, string hostname, SocketException innerException): base(message, hostname, innerException) { }

}

/// <summary>
/// <para>Exception thrown when a command is sent to an outlet, but the outlet is missing the hardware functionality to fulfill the request.</para>
/// <para>For example, this will be thrown if you try to get an energy reading from an outlet like an EP10 that does not have an energy meter.</para>
/// <para>To check ahead of time if an outlet has a feature, you can call <c>(await IKasaOutlet.System.GetInfo()).Features</c>.</para>
/// </summary>
public class FeatureUnavailable: KasaException {

    /// <summary>
    /// The method (<c>family.command</c>) that was sent to the outlet
    /// </summary>
    public string RequestMethod { get; }

    /// <summary>
    /// The feature that is required to call the given command.
    /// </summary>
    public Feature RequiredFeature { get; }

    /// <summary>
    /// <para>Exception thrown when a command is sent to an outlet, but the outlet is missing the hardware functionality to fulfill the request.</para>
    /// <para>For example, this will be thrown if you try to get an energy reading from an outlet like an EP10 that does not have an energy meter.</para>
    /// <para>To check ahead of time if an outlet has a feature, you can call <c>(await IKasaOutlet.System.GetInfo()).Features</c>.</para>
    /// </summary>
    public FeatureUnavailable(string requestMethod, Feature requiredFeature, string hostname): base(
        $"The Kasa device {hostname} is missing the {requiredFeature} feature, which is required to run the {requestMethod} command. You can check {nameof(IKasaOutlet)}.{CommandFamily.System}.{nameof(IKasaOutlet.ISystemCommands.GetInfo)}().Features to see which features your device has.",
        hostname) {
        RequestMethod   = requestMethod;
        RequiredFeature = requiredFeature;
        HResult         = -1;
    }

}

/// <summary>
/// <para>The outlet sent a JSON response that could not be parsed into the expected response object.</para>
/// <para>This may indicate a defect in this library or an API change in the outlet firmware.</para>
/// </summary>
public class ResponseParsingException: KasaException {

    /// <summary>
    /// The method (<c>family.command</c>) that was sent to the outlet
    /// </summary>
    public string RequestMethod { get; }

    /// <summary>
    /// The deciphered response JSON string that could not be parsed into an object.
    /// </summary>
    public string Response { get; }

    /// <summary>
    /// The .NET class that the parser unsuccessfully attempted to deserialize the JSON string into.
    /// </summary>
    public Type ResponseType { get; }

    /// <summary>
    /// <para>The outlet sent a JSON response that could not be parsed into the expected response object.</para>
    /// <para>This may indicate a defect in this library or an API change in the outlet firmware.</para>
    /// </summary>
    public ResponseParsingException(string requestMethod, string response, Type responseType, string hostname, Exception innerException): base(
        $"Failed to deserialize JSON to {responseType}: {response}", hostname, innerException) {
        RequestMethod = requestMethod;
        Response      = response;
        ResponseType  = responseType;
    }

}

// public class KasaArgumentException: KasaException {
//
//     public KasaArgumentException(string requestMethod, string hostname): base($"Invalid arguments passed to {requestMethod}", hostname) {
//         HResult = -3;
//     }
//
// }