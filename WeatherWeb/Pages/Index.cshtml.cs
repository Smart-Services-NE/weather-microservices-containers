using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WeatherWeb.Pages;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _clientFactory;

    public IndexModel(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    [BindProperty(SupportsGet = true)]
    public string? ZipCode { get; set; }

    public WeatherForecast? Forecast { get; set; }

    public async Task OnGetAsync()
    {
        ZipCode ??= "90210";
        var client = _clientFactory.CreateClient("WeatherApi");
        try 
        {
            Forecast = await client.GetFromJsonAsync<WeatherForecast>($"weatherforecast?zipcode={ZipCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching weather: {ex.Message}");
        }
    }
}

public record WeatherForecast(string City, string State, string ZipCode, int TemperatureF, string Summary, string Date);
