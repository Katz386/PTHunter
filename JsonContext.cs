using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PTHunter
{
    [JsonSerializable(typeof(Dictionary<string, string>))]
    internal partial class HeaderJsonContext : JsonSerializerContext
    {
    }
}
