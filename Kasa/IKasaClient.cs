using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kasa;

internal interface IKasaClient: IDisposable {

    string Hostname { get; }
    bool Connected { get; }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SocketException"></exception>
    Task Connect();

    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="JsonReaderException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    Task<T> Send<T>(CommandFamily commandFamily, string methodName, object? parameters = null);

}