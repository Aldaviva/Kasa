using Kasa;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Test;

public class KasaOutletTest: AbstractKasaOutletTest {

    [Fact]
    public void Hostname() {
        KasaOutlet outlet = new("localhost");
        outlet.Hostname.Should().Be("localhost");
    }

    [Fact]
    public void Connect() {
        Outlet.Connect();
        A.CallTo(() => Client.Connect()).MustHaveHappened();
    }

    [Fact]
    public void Dispose() {
        Outlet.Dispose();
        A.CallTo(() => Client.Dispose()).MustHaveHappened();
    }

    [Fact]
    public void Options() {
        KasaClient       client        = new("127.0.0.1");
        ILoggerFactory   loggerFactory = new NullLoggerFactory();
        using KasaOutlet outlet        = new(client) { Options = new Options { LoggerFactory = loggerFactory } };
        outlet.Options.LoggerFactory.Should().BeSameAs(loggerFactory);

        loggerFactory                = new NullLoggerFactory();
        outlet.Options.LoggerFactory = loggerFactory;
        outlet.Options.LoggerFactory.Should().BeSameAs(loggerFactory);

        outlet.Options.MaxAttempts = 8;
        outlet.Options.MaxAttempts.Should().Be(8);

        outlet.Options.ReceiveTimeout = TimeSpan.FromSeconds(17);
        outlet.Options.ReceiveTimeout.Should().Be(TimeSpan.FromSeconds(17));

        outlet.Options.SendTimeout = TimeSpan.FromSeconds(17);
        outlet.Options.SendTimeout.Should().Be(TimeSpan.FromSeconds(17));

        outlet.Options.RetryDelay = TimeSpan.FromSeconds(88);
        outlet.Options.RetryDelay.Should().Be(TimeSpan.FromSeconds(88));
    }

}