using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Responses;

public class ApiInformationResponse : ApiResponseBase<Dictionary<string, ApiInformation>>
{
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