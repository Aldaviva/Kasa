using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutletBase.ICloudCommands Cloud => this;

    /// <inheritdoc />
    async Task<bool> IKasaOutletBase.ICloudCommands.IsConnectedToCloudAccount() {
        JObject response = await Client.Send<JObject>(CommandFamily.Cloud, "get_info").ConfigureAwait(false);
        return response.Value<int?>("binded") == 1;
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ICloudCommands.DisconnectFromCloudAccount() {
        return Client.Send<JObject>(CommandFamily.Cloud, "unbind");
    }

}