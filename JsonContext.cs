using System.Text.Json.Serialization;

namespace PTHunter
{
    [JsonSerializable(typeof(Dictionary<string, string>))]
    internal partial class HeaderJsonContext : JsonSerializerContext
    {
    }
}
