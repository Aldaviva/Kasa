using Kasa;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Test;

public class KasaExceptionTest {

    [Fact]
    public void FeatureUnavailable() {
        FeatureUnavailable exception = new("Family.method", Feature.EnergyMeter, "hostname");
        exception.RequestMethod.Should().Be("Family.method");
        exception.RequiredFeature.Should().Be(Feature.EnergyMeter);
        exception.Hostname.Should().Be("hostname");
    }

    [Fact]
    public void ResponseParsingException() {
        ResponseParsingException exception = new("Family.method", "<invalid json>", typeof(JObject), "hostname", new JsonReaderException("inner"));
        exception.RequestMethod.Should().Be("Family.method");
        exception.Response.Should().Be("<invalid json>");
        exception.ResponseType.Should().Be(typeof(JObject));

    }

}