using System.Text.Json;
using WeatherApp.Models;

namespace WeatherApp.Services;

public class WeatherService
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://api.openweathermap.org"),
        Timeout = TimeSpan.FromSeconds(15)
    };

    private const string ApiKey = "deeb205457c7bb153b4448be094d342c";
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<WeatherResponse> GetWeatherAsync(double lat, double lon)
    {
        var url = $"/data/2.5/weather?lat={lat}&lon={lon}&appid={ApiKey}&units=metric";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<WeatherResponse>(json, _jsonOptions)
               ?? throw new InvalidOperationException("Failed to parse weather response.");
    }

    public async Task<ForecastResponse> GetForecastAsync(double lat, double lon)
    {
        var url = $"/data/2.5/forecast?lat={lat}&lon={lon}&appid={ApiKey}&units=metric&cnt=6";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ForecastResponse>(json, _jsonOptions)
               ?? throw new InvalidOperationException("Failed to parse forecast response.");
    }
}
