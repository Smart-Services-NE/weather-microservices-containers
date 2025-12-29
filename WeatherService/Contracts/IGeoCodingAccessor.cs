namespace WeatherService.Contracts;

public interface IGeoCodingAccessor
{
    Task<GeoCodingResult> GetLocationByZipCodeAsync(string zipCode);
}
