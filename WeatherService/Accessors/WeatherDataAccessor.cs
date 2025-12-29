using System.Net;
using System.Net.Http.Json;
using WeatherService.Contracts;
using WeatherService.Accessors.Models;

namespace WeatherService.Accessors;

public class WeatherDataAccessor : IWeatherDataAccessor
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WeatherDataAccessor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WeatherDataResult> GetCurrentWeatherAsync(string latitude, string longitude)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true&temperature_unit=fahrenheit";

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new WeatherDataResult(
                    false,
                    null,
                    null,
                    new ErrorInfo("WEATHER_API_ERROR", $"Weather API returned status code: {response.StatusCode}")
                );
            }

            var weatherData = await response.Content.ReadFromJsonAsync<OpenMeteoResponse>();

            if (weatherData == null)
            {
                return new WeatherDataResult(
                    false,
                    null,
                    null,
                    new ErrorInfo("WEATHER_NO_DATA", "No weather data received from API")
                );
            }

            return new WeatherDataResult(
                true,
                weatherData.CurrentWeather.Temperature,
                weatherData.CurrentWeather.WeatherCode,
                null
            );
        }
        catch (HttpRequestException ex)
        {
            return new WeatherDataResult(
                false,
                null,
                null,
                new ErrorInfo("WEATHER_NETWORK_ERROR", $"Network error calling weather API: {ex.Message}")
            );
        }
        catch (Exception ex)
        {
            return new WeatherDataResult(
                false,
                null,
                null,
                new ErrorInfo("WEATHER_UNEXPECTED_ERROR", $"Unexpected error: {ex.Message}")
            );
        }
    }
}
