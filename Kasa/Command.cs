using Newtonsoft.Json.Linq;

namespace Kasa; 
class Command {
    public CommandFamily Family { get; }
    public string MethodName { get; }
    internal JObject Json { get; }

    public Command(CommandFamily family, string methodName, object? parameters = null) {
        if (string.IsNullOrEmpty(methodName)) throw new System.ArgumentException($"'{nameof(methodName)}' cannot be null or empty.", nameof(methodName));
        Family = family;
        MethodName = methodName;

        Json = new(new JProperty(family.ToJsonString(), new JObject(
            new JProperty(methodName, parameters is null ? null : JObject.FromObject(parameters)))));
    }
}
