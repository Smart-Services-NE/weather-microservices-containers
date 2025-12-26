using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using WeatherWeb.Models;

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
        ZipCode ??= "68136";
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
