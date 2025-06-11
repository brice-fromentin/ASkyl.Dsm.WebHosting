using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationAcl : IGenericCloneable<FileStationAcl>
{
    [JsonPropertyName("append")]
    public bool? Append { get; set; }

    [JsonPropertyName("del")]
    public bool? Delete { get; set; }

    [JsonPropertyName("exec")]
    public bool? Execute { get; set; }

    [JsonPropertyName("read")]
    public bool? Read { get; set; }

    [JsonPropertyName("write")]
    public bool? Write { get; set; }

    public FileStationAcl Clone()
        => new()
        {
            Append = this.Append,
            Delete = this.Delete,
            Execute = this.Execute,
            Read = this.Read,
            Write = this.Write
        };
}
