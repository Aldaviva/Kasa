using Kasa;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;

namespace Test;

public class KasaMultiOutletTest: IDisposable {

    private readonly IKasaClient     _client = A.Fake<IKasaClient>();
    private readonly KasaMultiOutlet _ep40;

    public KasaMultiOutletTest() {
        _ep40 = new KasaMultiOutlet(_client);

        A.CallTo(() => _client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo", null, null)).Returns(new SystemInfo {
            Children = [
                new ChildOutlet {
                    Id         = "800648C61B22DD1DE8AFD8858B29192022087E7200",
                    IsOutletOn = false,
                    Name       = "Outlet 1"
                },
                new ChildOutlet {
                    Id         = "800648C61B22DD1DE8AFD8858B29192022087E7201",
                    IsOutletOn = true,
                    Name       = "Outlet 2"
                }
            ],
            DeviceId               = "800648C61B22DD1DE8AFD8858B29192022087E72",
            Features               = new HashSet<Feature>([Feature.Timer]),
            HardwareId             = "B3B7B05B758C3EDA8F9C69FECDBA2111",
            HardwareVersion        = "1.0",
            IndicatorLightDisabled = false,
            MacAddress             = new PhysicalAddress([0xF0, 0xA7, 0x31, 0xC6, 0x48, 0x65]),
            ModelFamily            = null,
            ModelName              = "EP40(US)",
            Name                   = "EP40",
            OemId                  = "2F9215F1DCBF7DC17F80E2B0CACD47FC",
            OperatingMode          = OperatingMode.None,
            SignalStrength         = -64,
            SoftwareVersion        = "1.0.4 Build 240305 Rel.111944",
            Updating               = false
        });
    }

    [Fact]
    public async Task GetInfo() {
        await _ep40.System.GetInfo();

        (await _ep40.System.CountOutlets()).Should().Be(2);

        A.CallTo(() => _client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo", null, null)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task IsOutletOn() {
        (await _ep40.System.IsOutletOn(0)).Should().BeFalse();
        (await _ep40.System.IsOutletOn(1)).Should().BeTrue();

        A.CallTo(() => _client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo", null, null)).MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task SetOutletOn() {
        A.CallTo(() => _client.Send<JObject>(CommandFamily.System, "set_relay_state", A<object?>._, A<object?>._)).Returns(JObject.Parse("""{"set_relay_state":{"err_code":0}}"""));

        await _ep40.System.SetOutletOn(0, false);
        await _ep40.System.SetOutletOn(1, true);

        A.CallTo(() => _client.Send<JObject>(CommandFamily.System, "set_relay_state", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { state = 0 }, "")),
            new ChildContext("800648C61B22DD1DE8AFD8858B29192022087E7200"))).MustHaveHappened();
        A.CallTo(() => _client.Send<JObject>(CommandFamily.System, "set_relay_state", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { state = 1 }, "")),
            new ChildContext("800648C61B22DD1DE8AFD8858B29192022087E7201"))).MustHaveHappened();
    }

    [Fact]
    public async Task GetName() {
        (await _ep40.System.GetName(0)).Should().Be("Outlet 1");
    }

    [Fact]
    public async Task SetName() {
        await _ep40.System.SetName(0, "Outlet A");

        A.CallTo(() => _client.Send<JObject>(CommandFamily.System, "set_dev_alias", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { alias = "Outlet A" }, "")),
            new ChildContext("800648C61B22DD1DE8AFD8858B29192022087E7200"))).MustHaveHappened();
    }

    [Fact]
    public void ChildContextEquality() {
        ChildContext a = new("800648C61B22DD1DE8AFD8858B29192022087E7200");
        ChildContext b = new("800648C61B22DD1DE8AFD8858B29192022087E7200");
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.ToString().Should().Be("childId: 800648C61B22DD1DE8AFD8858B29192022087E7200");
    }

    public void Dispose() {
        _ep40.Dispose();
        GC.SuppressFinalize(this);
    }

}