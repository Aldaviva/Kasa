using Kasa;
using Newtonsoft.Json.Linq;

namespace Test;

public class KasaCloudTest: AbstractKasaOutletTest {

    [Fact]
    public async Task IsConnectedTrue() {
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Cloud, "get_info", null, null)).Returns(JObject.Parse(
            """
            {"username": "user@email.com", "server": "n-devs.tplinkcloud.com", "binded": 1, "cld_connection": 0, "illegalType": 0, "stopConnect": 0, "tcspStatus": 0, "fwDlPage": "", "tcspInfo": "", "fwNotifyType": -1}
            """));

        bool actual = await Outlet.Cloud.IsConnectedToCloudAccount();
        actual.Should().BeTrue();
    }

    [Fact]
    public async Task IsConnectedFalse() {
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Cloud, "get_info", null, null)).Returns(JObject.Parse(
            """
            {"username": "", "server": "n-devs.tplinkcloud.com", "binded": 0, "cld_connection": 1, "illegalType": 0, "stopConnect": 0, "tcspStatus": 1, "fwDlPage": "", "tcspInfo": "", "fwNotifyType": -1}
            """));

        bool actual = await Outlet.Cloud.IsConnectedToCloudAccount();
        actual.Should().BeFalse();
    }

    [Fact]
    public async Task Disconnect() {
        // This isn't the correct response because my firewall was blocking outlets' access to the Internet and disabling it only took effect when they rebooted and were already logged out, but since we don't inspect the response anyway this is sufficient.
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Cloud, "unbind", null, null)).Returns(JObject.Parse(
            """
            {"err_code": -24, "err_msg": "no reply from server"}
            """));

        await Outlet.Cloud.DisconnectFromCloudAccount();
    }

    [Fact]
    public async Task DisconnectIdempotent() {
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Cloud, "unbind", null, null)).Returns(JObject.Parse(
            """
            {"err_code": -8, "err_msg": "not bind yet"}
            """));

        await Outlet.Cloud.DisconnectFromCloudAccount();
    }

}