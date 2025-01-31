using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Kasa;

/// <summary>
/// Non-required configuration parameters for the <see cref="IKasaOutlet"/>, which can be used to fine-tune its behavior.
/// </summary>
public class Options: INotifyPropertyChanged {

    private ILoggerFactory? _loggerFactory;
    private uint?           _maxAttempts    = 10; // 20 seconds, enough to cover an EP10 rebooting in 14 seconds
    private TimeSpan        _receiveTimeout = TimeSpan.FromSeconds(2);
    private TimeSpan        _retryDelay     = TimeSpan.FromSeconds(1);
    private TimeSpan        _sendTimeout    = TimeSpan.FromSeconds(2);

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// <para>Allows you to optionally provide a <c>Microsoft.Extensions.Logging</c> <see cref="ILoggerFactory"/> so this <see cref="IKasaOutlet"/> can emit log messages.</para>
    /// <para>By default, this property is <c>null</c>, and logs are not emitted.</para>
    /// <para>Setting this is useful if you want to see the raw JSON messages being sent and received from the outlet's TCP server, which are emitted at <see cref="LogLevel.Trace"/> level.</para>
    /// <para>To get an <see cref="ILoggerFactory"/> instance, you can use .NET dependency injection to call <c>IServiceCollection.GetService&lt;ILoggerFactory&gt;()</c>, or you can create one manually by calling <c>LoggerFactory.Create(ILoggingBuilder)</c>.</para>
    /// <para>For more information, see https://docs.microsoft.com/en-us/dotnet/core/extensions/logging</para>
    /// </summary>
    public ILoggerFactory? LoggerFactory {
        get => _loggerFactory;
        set {
            if (!Equals(value, _loggerFactory)) {
                _loggerFactory = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// <para>The number of attempts that will be made to send a given command before giving up when exceptions are encountered.</para>
    /// <para>The default value is <c>10</c> attempts, which means 1 initial attempt and up to 9 retries on failure.</para>
    /// <para>The minimum value is <c>1</c>, and the maximum value is <see cref="uint.MaxValue"/>. If you set this property to 0, it will behave as if it is set to 1 (which means 1 attempt and 0 retries).</para>
    /// <para>If you set this property to <see langword="null"/>, the client will retry a failing request infinitely, no matter how many times it fails.</para>
    /// <para>The client will wait <see cref="RetryDelay"/> before each retry.</para>
    /// <para>Only temporary exceptions trigger retries, such as timeouts and refused connections, but not JSON errors or <see cref="ObjectDisposedException"/>, which would just fail again no matter how many times they are retried.</para>
    /// </summary>
    public uint? MaxAttempts {
        get => _maxAttempts;
        set {
            if (value != _maxAttempts) {
                _maxAttempts = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// <para>The amount of time to wait before retrying to send each command after a failure.</para>
    /// <para>The default value is <c>1</c> second.</para>
    /// <para>The minimum value is <see cref="TimeSpan.Zero"/>, and the maximum value is <c>TimeSpan.FromMilliseconds(int.MaxValue)</c>, (or <c>TimeSpan.FromMilliseconds(uint.MaxValue-1)</c> starting in .NET 6). Values outside the valid range will be clipped to fit in the range.</para>
    /// </summary>
    public TimeSpan RetryDelay {
        get => _retryDelay;
        set {
            if (!value.Equals(_retryDelay)) {
                _retryDelay = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// <para>How long to wait during a read operation on the TCP socket.</para>
    /// <para>The default value is <c>2</c> seconds.</para>
    /// <para>The minimum value is <see cref="TimeSpan.Zero"/>, and the maximum value is <c>TimeSpan.FromMilliseconds(int.MaxValue)</c>, which is about 24 days and 21 hours. Values outside the valid range will be clipped to fit in the range.</para>
    /// </summary>
    public TimeSpan ReceiveTimeout {
        get => _receiveTimeout;
        set {
            if (!value.Equals(_receiveTimeout)) {
                _receiveTimeout = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// <para>How long to wait during a write operation on the TCP socket.</para>
    /// <para>The default value is <c>2</c> seconds.</para>
    /// <para>The minimum value is <see cref="TimeSpan.Zero"/>, and the maximum value is <c>TimeSpan.FromMilliseconds(int.MaxValue)</c>, which is about 24 days and 21 hours. Values outside the valid range will be clipped to fit in the range.</para>
    /// </summary>
    public TimeSpan SendTimeout {
        get => _sendTimeout;
        set {
            if (!value.Equals(_sendTimeout)) {
                _sendTimeout = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

}