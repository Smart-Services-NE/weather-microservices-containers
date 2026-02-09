using System.Net;
using System.Net.Http.Json;
using WeatherService.Contracts;
using WeatherService.Accessors.Models;

namespace WeatherService.Accessors;

public class WeatherDataAccessor : IWeatherDataAccessor
{
    private readonly HttpClient _httpClient;

    public WeatherDataAccessor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeatherDataResult> GetCurrentWeatherAsync(string latitude, string longitude)
    {
        try
        {
            var url = $"v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true&temperature_unit=fahrenheit&hourly=temperature_2m,weathercode&daily=temperature_2m_max,temperature_2m_min,weathercode&timezone=auto";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new WeatherDataResult(
                    false,
                    null,
                    null,
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
                    null,
                    null,
                    new ErrorInfo("WEATHER_NO_DATA", "No weather data received from API")
                );
            }

            var hourlyForecasts = weatherData.Hourly?.Time.Select((time, index) => new HourlyForecast(
                time,
                weatherData.Hourly.Temperature2m[index],
                weatherData.Hourly.WeatherCode[index],
                string.Empty // To be filled by manager
            )).Take(24);

            var dailyForecasts = weatherData.Daily?.Time.Select((time, index) => new DailyForecast(
                time,
                weatherData.Daily.Temperature2mMax[index],
                weatherData.Daily.Temperature2mMin[index],
                weatherData.Daily.WeatherCode[index],
                string.Empty // To be filled by manager
            ));

            return new WeatherDataResult(
                true,
                weatherData.CurrentWeather.Temperature,
                weatherData.CurrentWeather.WeatherCode,
                hourlyForecasts,
                dailyForecasts,
                null
            );
        }
        catch (HttpRequestException ex)
        {
            return new WeatherDataResult(
                false,
                null,
                null,
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
                null,
                null,
                new ErrorInfo("WEATHER_UNEXPECTED_ERROR", $"Unexpected error: {ex.Message}")
            );
        }
    }
}
