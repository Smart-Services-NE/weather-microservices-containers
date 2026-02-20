using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapr.Client;

using WeatherWeb.Models;

namespace WeatherWeb.Pages;

public class IndexModel : PageModel
{
    private readonly DaprClient _daprClient;

    public IndexModel(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    [BindProperty(SupportsGet = true)]
    public string? ZipCode { get; set; }

    public WeatherForecast? Forecast { get; set; }

    public async Task OnGetAsync()
    {
        ZipCode ??= "68136";
        try
        {
            Forecast = await _daprClient.InvokeMethodAsync<WeatherForecast>(
                HttpMethod.Get,
                "weather-api",
                $"api/weather/forecast?zipcode={ZipCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching weather: {ex}");
        }
    }
}
