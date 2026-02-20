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

    [BindProperty]
    public string? SubscriberEmail { get; set; }

    public WeatherForecast? Forecast { get; set; }

    public string? SubscriptionMessage { get; set; }

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

    public async Task<IActionResult> OnPostSubscribeAsync()
    {
        if (string.IsNullOrEmpty(SubscriberEmail) || string.IsNullOrEmpty(ZipCode))
        {
            SubscriptionMessage = "Email and Zip Code are required.";
            return Page();
        }

        try
        {
            var request = new { Email = SubscriberEmail, ZipCode = ZipCode };
            await _daprClient.InvokeMethodAsync(
                HttpMethod.Post,
                "weather-api",
                "api/weather/subscriptions",
                request);

            SubscriptionMessage = "Successfully subscribed to alerts!";
            SubscriberEmail = string.Empty; // Clear form
        }
        catch (Exception ex)
        {
            SubscriptionMessage = $"Error subscribing: {ex.Message}";
        }

        await OnGetAsync(); // Reload forecast
        return Page();
    }
}
