using FluentAssertions;
using Kasa.Log;
using slf4net.Factories;

namespace Test;

public class LoggerFactoryResolverTest {

    [Fact]
    public void GetFactory() {
        NOPLoggerFactory factory = NOPLoggerFactory.Instance;

        LoggerFactoryResolver loggerFactoryResolver = new(factory);

        loggerFactoryResolver.GetFactory().Should().BeSameAs(factory);
    }

}