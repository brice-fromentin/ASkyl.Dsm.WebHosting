using System;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API;

public class SynoInformationResponse : ApiResponse<Dictionary<string, SynoInformation>>
{
}

public class SynoInformation
{
        [JsonPropertyName("maxVersion")]
        public int MaxVersion { get; set; }

        [JsonPropertyName("minVersion")]
        public int MinVersion { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; } = "";
}
/*
{
    "data": {
        "SYNO.API.Auth": {
            "maxVersion": 7,
            "minVersion": 1,
            "path": "entry.cgi"
        }
    },
    "success": true
}
*/