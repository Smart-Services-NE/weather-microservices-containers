using System.Net;
using System.Net.Http.Json;
using WeatherService.Contracts;
using WeatherService.Accessors.Models;

namespace WeatherService.Accessors;

public class GeoCodingAccessor : IGeoCodingAccessor
{
    private readonly HttpClient _httpClient;

    public GeoCodingAccessor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GeoCodingResult> GetLocationByZipCodeAsync(string zipCode)
    {
        try
        {
            var response = await _httpClient.GetAsync($"us/{zipCode}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new GeoCodingResult(
                        false,
                        null,
                        null,
                        null,
                        null,
                        new ErrorInfo("ZIPCODE_NOT_FOUND", $"Zipcode '{zipCode}' not found")
                    );
                }

                return new GeoCodingResult(
                    false,
                    null,
                    null,
                    null,
                    null,
                    new ErrorInfo("GEOCODING_API_ERROR", $"Geocoding API returned status code: {response.StatusCode}")
                );
            }

            var geoData = await response.Content.ReadFromJsonAsync<ZipCodeResponse>();

            if (geoData == null || geoData.Places.Count == 0)
            {
                return new GeoCodingResult(
                    false,
                    null,
                    null,
                    null,
                    null,
                    new ErrorInfo("GEOCODING_NO_DATA", $"No location data found for zipcode '{zipCode}'")
                );
            }

            var place = geoData.Places[0];

            return new GeoCodingResult(
                true,
                place.PlaceName,
                place.StateAbbreviation,
                place.Latitude,
                place.Longitude,
                null
            );
        }
        catch (HttpRequestException ex)
        {
            return new GeoCodingResult(
                false,
                null,
                null,
                null,
                null,
                new ErrorInfo("GEOCODING_NETWORK_ERROR", $"Network error calling geocoding API: {ex.Message}")
            );
        }
        catch (Exception ex)
        {
            return new GeoCodingResult(
                false,
                null,
                null,
                null,
                null,
                new ErrorInfo("GEOCODING_UNEXPECTED_ERROR", $"Unexpected error: {ex.Message}")
            );
        }
    }
}
