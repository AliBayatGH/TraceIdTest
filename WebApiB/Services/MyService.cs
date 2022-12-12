using Microsoft.Extensions.Options;
using WebApiB.Options;

namespace WebApiB.Services;

public class MyService : IMyService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<UrlsOptions> _urlsOptions;

    public MyService(HttpClient httpClient, IOptions<UrlsOptions> urlsOptions)
    {
        _httpClient = httpClient;
        _urlsOptions = urlsOptions;
    }
    public async Task<string?> GetAsync()
    {
        var result = await _httpClient.GetStringAsync($"{_urlsOptions.Value.MyServiceUrl}/WeatherForecast");

        return result;
    }
}
