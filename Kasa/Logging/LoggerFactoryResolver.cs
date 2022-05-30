using slf4net;

namespace Kasa.Logging;

public record LoggerFactoryResolver(ILoggerFactory Factory): IFactoryResolver {

    public ILoggerFactory Factory { get; } = Factory;

    public ILoggerFactory GetFactory() => Factory;

}