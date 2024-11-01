using Newtonsoft.Json;

namespace Kasa;

internal readonly struct ChildContext(string childId) {

    [JsonProperty("child_ids")]
    public IEnumerable<string> ChildIds => [childId];

    public override bool Equals(object? obj) => obj is ChildContext other && childId.Equals(other.ChildIds.First(), StringComparison.Ordinal);

    public override int GetHashCode() => childId.GetHashCode();

    public override string ToString() => $"{nameof(childId)}: {childId}";

}