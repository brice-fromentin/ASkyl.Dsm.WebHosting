namespace Askyl.Dsm.WebHosting.Data.Runtime;

public class FrameworkInfo(string type = "", string version = "")
{
    public string Type { get; set; } = type;
    public string Version { get; set; } = version;
}
