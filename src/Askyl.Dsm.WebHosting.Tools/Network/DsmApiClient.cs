using System.Net.Http.Json;
using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;
using Askyl.Dsm.WebHosting.Data.API.Parameters;
using Askyl.Dsm.WebHosting.Data.API.Parameters.AuthenticationAPI;
using Askyl.Dsm.WebHosting.Data.API.Parameters.InformationsAPI;
using Askyl.Dsm.WebHosting.Data.API.Responses;
using Askyl.Dsm.WebHosting.Data.Security;

namespace Askyl.Dsm.WebHosting.Tools.Network;

public class DsmApiClient(IHttpClientFactory HttpClientFactory)
{
    private readonly HttpClient _httpClient = HttpClientFactory.CreateClient(DsmDefaults.HttpClientName);

    private string _server = "";
    private int _port = DsmDefaults.DefaultHttpsPort;
    private string _sid = "";

    public ApiInformationCollection ApiInformations { get; } = new();

    public bool IsConnected => !String.IsNullOrEmpty(_sid);

    public async Task<bool> ConnectAsync(LoginModel model)
    {
        if (!ReadSettings())
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
        var parameters = new InformationsQueryParameters(this.ApiInformations);

        var result = await ExecuteAsync<ApiInformationResponse>(parameters);

        if (result is null || !result.Success || result.Data is null || result.Data.Count == 0)
        {
            return false;
        }

        ApiInformations.Replace(result.Data);

        return true;
    }

    private async Task<bool> AuthenticateAsync(LoginModel model)
    {
        var parameters = new AuthenticationLoginParameters(this.ApiInformations);

        parameters.Parameters.Account = model.Login;
        parameters.Parameters.Password = model.Password;
        parameters.Parameters.OtpCode = model.OtpCode;

        var response = await ExecuteAsync<SynoLoginResponse>(parameters);

        if (response == null || !response.Success || response.Data == null)
        {
            return false;
        }

        _sid = response.Data.Sid;
        _httpClient.DefaultRequestHeaders.Add("Cookie", "_SSID=" + response.Data.Sid);

        return true;
    }

    #region HTTP Request calls

    public async Task<R?> ExecuteAsync<R>(IApiParameters parameters)
    {
        var url = parameters.BuildUrl(_server, _port);

        return parameters.SerializationFormat switch
        {
            SerializationFormats.Query
                => await ExecuteQueryAsync<R>(url),
            SerializationFormats.Form
                => await ExecuteFormAsync<R>(url, parameters),
            SerializationFormats.Json
                => await ExecuteJsonAsync<R>(url, parameters),
            _
                => throw new NotSupportedException($"SerializationFormat : {parameters.SerializationFormat} not supported.")
        };
    }

    private async Task<R?> ExecuteQueryAsync<R>(string url)
        => await _httpClient.GetFromJsonAsync<R>(url);

    private async Task<R?> ExecuteFormAsync<R>(string url, IApiParameters parameters)
        => await ExecutePostAsync<R>(url, new StringContent(parameters.ToForm()));

    private async Task<R?> ExecuteJsonAsync<R>(string url, IApiParameters parameters)
        => await ExecutePostAsync<R>(url, new StringContent(parameters.ToJson()));

    private async Task<R?> ExecutePostAsync<R>(string url, HttpContent content)
    {
        var message = await _httpClient.PostAsync(url, content);

        if (message.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return default;
        }

        return await message.Content.ReadFromJsonAsync<R>();
    }

    #endregion
}
