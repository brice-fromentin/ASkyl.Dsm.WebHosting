using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Domain.Runtime;

public class FrameworkInfo(string type = "", string version = "")
{
    [JsonConstructor]
    public FrameworkInfo() : this("", "") { }

    public string Type { get; set; } = type;
    public string Version { get; set; } = version;
}
