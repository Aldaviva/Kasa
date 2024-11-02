using Newtonsoft.Json;

namespace Kasa;

internal readonly struct SocketContext(string socketId) {

    [JsonProperty("child_ids")]
    public IEnumerable<string> SocketIds => [socketId];

    public override bool Equals(object? obj) => obj is SocketContext other && socketId.Equals(other.SocketIds.First(), StringComparison.Ordinal);

    public override int GetHashCode() => socketId.GetHashCode();

    public override string ToString() => $"{nameof(socketId)}: {socketId}";

}