using System;
using System.Threading.Tasks;

namespace Kasa;

internal interface IKasaClient: IDisposable {

    string Hostname { get; }
    Options Options { get; set; }

    bool Connected { get; }

    /// <exception cref="ObjectDisposedException">This instance has already been disposed.</exception>
    /// <exception cref="NetworkException">The TCP connection failed.</exception>
    Task Connect();

    /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
    /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
    Task<T> Send<T>(CommandFamily commandFamily, string methodName, object? parameters = null);

}