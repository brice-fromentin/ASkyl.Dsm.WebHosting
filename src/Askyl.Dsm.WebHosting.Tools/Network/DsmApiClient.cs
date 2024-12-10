using System.Net.Http.Json;
using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API;
using Askyl.Dsm.WebHosting.Data.Security;

namespace Askyl.Dsm.WebHosting.Tools.Network;

public class DsmApiClient(IHttpClientFactory HttpClientFactory)
{
    private readonly HttpClient _httpClient = HttpClientFactory.CreateClient(DsmDefaults.HttpClientName);

    private string _server = "";
    private int _port = DsmDefaults.DefaultHttpsPort;
    private string _sid = "";
    private Dictionary<string, SynoInformation> _apiInfo = [];

    public async Task<bool> ConnectAsync(LoginModel model)
    {
        if(!ReadSettings())
        {
            return false;
        }

        if (!await HandShakeAsync())
        {
            return false;
        }

        if (!await AuthenticateAsync(model))
        {
            return false;
        }

        return true;
    }
    
    private bool ReadSettings()
    {
        var settings = File.ReadAllLines("/etc/synoinfo.conf")
                           .Where(x => x.Contains("="))
                           .ToDictionary(key => key.Split('=')[0], value => value.Split('=')[1].Replace("\"", ""));
        
        _server = settings[DsmDefaults.KeyExternalHostIp];

        if (!Int32.TryParse(settings[DsmDefaults.KeyExternalHttpsPort], out _port))
        {
            _port = DsmDefaults.DefaultHttpsPort;
        }

        return true;
    }

    private async Task<bool> HandShakeAsync()
    {
        var parameters = ApiParameters.Create(DsmDefaults.DsmApiInfo, DsmDefaults.DsmApiHandshakePath, 1, "query", [new("query", DsmDefaults.RequiredApiJoined)]);
        var response = await ExecuteAsync<SynoInformationResponse>(parameters);

        if (response == null || !response.Success)
        {
            return false;
        }

        _apiInfo = response.Data ?? [];

        return true;
    }

    private async Task<bool> AuthenticateAsync(LoginModel model)
    {
        var apiInfo = _apiInfo[DsmDefaults.DsmApiAuth];
        if (apiInfo == null)
        {
            return false;
        }
        
        var parameters = ApiParameters.Create(DsmDefaults.DsmApiAuth, apiInfo.Path, 6, "login", [new("account", model.Login), new("passwd", Uri.EscapeDataString(model.Password)), new("format", "sid")]);
        var response = await ExecuteAsync<SynoLoginResponse>(parameters);

        if (response == null || !response.Success || response.Data == null)
        {
            return false;
        }

        _sid = response.Data.Sid;

        return true;
    }

    private async Task<R?> ExecuteAsync<R>(ApiParameters parameters)
    {
        if (!String.IsNullOrEmpty(_sid))
        {
            parameters.Parameters["sid"] = _sid;
        }

        var url = BuildUrl(parameters);
        return await _httpClient.GetFromJsonAsync<R>(url);
    }

    private string BuildUrl(ApiParameters parameters)
    {
        var baseUrl = $"https://{_server}:{_port}/webapi/{parameters.Path}?api={parameters.Name}&version={parameters.Version}&method={parameters.Method}&";
        var queryString = string.Join("&", parameters.Parameters.Select(parameter => $"{parameter.Key}={parameter.Value}"));
        return baseUrl + queryString;
    }
}
