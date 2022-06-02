using slf4net;

namespace Kasa.Log;

/// <summary>
/// <para>Used to provide a logger factory to <c>slf4net</c> in console, GUI, and other non-ASP.NET appliations.</para>
/// <para>Example:</para>
/// <code>slf4net.LoggerFactory.SetFactoryResolver(new LoggerFactoryResolver(new slf4net.NLog.NLogLoggerFactory()));
/// slf4net.ILogger logger = slf4net.LoggerFactory.GetLogger(nameof(MyClass));
/// logger.Info("Hello");</code>
/// <para>If you use <c>log4net</c>, replace <c>NLogLoggerFactory</c> with <c>slf4net.log4net.Log4netLoggerFactory</c>.</para>
/// <para>For alternate <c>slf4net</c> configuration options, see https://github.com/ef-labs/slf4net/wiki/Configuration and https://github.com/ef-labs/slf4net/wiki/configuration-in-code</para>
/// </summary>
public class LoggerFactoryResolver: IFactoryResolver {

    private readonly ILoggerFactory _factory;

    /// <summary>
    /// <para>Used to provide a logger factory to <c>slf4net</c> in console, GUI, and other non-ASP.NET appliations.</para>
    /// <para>Example:</para>
    /// <code>slf4net.LoggerFactory.SetFactoryResolver(new LoggerFactoryResolver(new slf4net.NLog.NLogLoggerFactory()));
    /// slf4net.ILogger logger = slf4net.LoggerFactory.GetLogger(nameof(MyClass));
    /// logger.Info("Hello");</code>
    /// <para>If you use <c>log4net</c>, replace <c>NLogLoggerFactory</c> with <c>slf4net.log4net.Log4netLoggerFactory</c>.</para>
    /// <para>For alternate <c>slf4net</c> configuration options, see https://github.com/ef-labs/slf4net/wiki/Configuration and https://github.com/ef-labs/slf4net/wiki/configuration-in-code</para>
    /// </summary>
    /// <param name="factory">A logger factory from <c>slf4net</c>, usually an <c>NLogLoggerFactory</c> or <c>Log4netLoggerFactory</c></param>
    public LoggerFactoryResolver(ILoggerFactory factory) {
        _factory = factory;
    }

    /// <summary>
    /// Get the provided factory. To be called by <c>slf4net.LoggerFactory</c>.
    /// </summary>
    /// <returns>The same factory with which this instance was constructed.</returns>
    public ILoggerFactory GetFactory() => _factory;

}