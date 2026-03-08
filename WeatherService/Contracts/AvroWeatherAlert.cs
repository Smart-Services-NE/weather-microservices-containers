using global::Avro;
using global::Avro.Specific;

namespace com.weatherapp.notifications;

public enum AlertType
{
    SEVERE_WEATHER,
    TEMPERATURE_EXTREME,
    PRECIPITATION_HEAVY,
    WIND_WARNING,
    STORM_WARNING,
    GENERAL_ALERT
}

public enum Severity
{
    INFO,
    WARNING,
    SEVERE,
    CRITICAL
}

public partial class Location : ISpecificRecord
{
    public static Schema _SCHEMA = Schema.Parse(@"{""type"":""record"",""name"":""Location"",""namespace"":""com.weatherapp.notifications"",""fields"":[{""name"":""zipCode"",""type"":""string""},{""name"":""city"",""type"":[""null"",""string""],""default"":null},{""name"":""state"",""type"":[""null"",""string""],""default"":null},{""name"":""latitude"",""type"":[""null"",""double""],""default"":null},{""name"":""longitude"",""type"":[""null"",""double""],""default"":null}]}");
    public string zipCode { get; set; } = null!;
    public string? city { get; set; }
    public string? state { get; set; }
    public double? latitude { get; set; }
    public double? longitude { get; set; }
    public virtual Schema Schema => _SCHEMA;
    public virtual object Get(int fieldPos)
    {
        switch (fieldPos)
        {
            case 0: return this.zipCode;
            case 1: return this.city;
            case 2: return this.state;
            case 3: return this.latitude;
            case 4: return this.longitude;
            default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
        };
    }
    public virtual void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: this.zipCode = (string)fieldValue; break;
            case 1: this.city = (string?)fieldValue; break;
            case 2: this.state = (string?)fieldValue; break;
            case 3: this.latitude = (double?)fieldValue; break;
            case 4: this.longitude = (double?)fieldValue; break;
            default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
        };
    }
}

public partial class WeatherConditions : ISpecificRecord
{
    public static Schema _SCHEMA = Schema.Parse(@"{""type"":""record"",""name"":""WeatherConditions"",""namespace"":""com.weatherapp.notifications"",""fields"":[{""name"":""currentTemperature"",""type"":[""null"",""double""],""default"":null},{""name"":""weatherCode"",""type"":[""null"",""int""],""default"":null},{""name"":""weatherDescription"",""type"":[""null"",""string""],""default"":null},{""name"":""windSpeed"",""type"":[""null"",""double""],""default"":null},{""name"":""precipitation"",""type"":[""null"",""double""],""default"":null}]}");
    public double? currentTemperature { get; set; }
    public int? weatherCode { get; set; }
    public string? weatherDescription { get; set; }
    public double? windSpeed { get; set; }
    public double? precipitation { get; set; }
    public virtual Schema Schema => _SCHEMA;
    public virtual object Get(int fieldPos)
    {
        switch (fieldPos)
        {
            case 0: return this.currentTemperature;
            case 1: return this.weatherCode;
            case 2: return this.weatherDescription;
            case 3: return this.windSpeed;
            case 4: return this.precipitation;
            default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
        };
    }
    public virtual void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: this.currentTemperature = (double?)fieldValue; break;
            case 1: this.weatherCode = (int?)fieldValue; break;
            case 2: this.weatherDescription = (string?)fieldValue; break;
            case 3: this.windSpeed = (double?)fieldValue; break;
            case 4: this.precipitation = (double?)fieldValue; break;
            default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
        };
    }
}

public partial class WeatherAlert : ISpecificRecord
{
    public static Schema _SCHEMA = Schema.Parse(@"{""type"":""record"",""name"":""WeatherAlert"",""namespace"":""com.weatherapp.notifications"",""fields"":[{""name"":""messageId"",""type"":""string""},{""name"":""subject"",""type"":""string""},{""name"":""body"",""type"":""string""},{""name"":""recipient"",""type"":""string""},{""name"":""timestamp"",""type"":{""type"":""long"",""logicalType"":""timestamp-millis""}},{""name"":""alertType"",""type"":{""type"":""enum"",""name"":""AlertType"",""symbols"":[""SEVERE_WEATHER"",""TEMPERATURE_EXTREME"",""PRECIPITATION_HEAVY"",""WIND_WARNING"",""STORM_WARNING"",""GENERAL_ALERT""]}},{""name"":""severity"",""type"":{""type"":""enum"",""name"":""Severity"",""symbols"":[""INFO"",""WARNING"",""SEVERE"",""CRITICAL""]}},{""name"":""location"",""type"":{""type"":""record"",""name"":""Location"",""fields"":[{""name"":""zipCode"",""type"":""string""},{""name"":""city"",""type"":[""null"",""string""],""default"":null},{""name"":""state"",""type"":[""null"",""string""],""default"":null},{""name"":""latitude"",""type"":[""null"",""double""],""default"":null},{""name"":""longitude"",""type"":[""null"",""double""],""default"":null}]}},{""name"":""weatherConditions"",""type"":{""type"":""record"",""name"":""WeatherConditions"",""fields"":[{""name"":""currentTemperature"",""type"":[""null"",""double""],""default"":null},{""name"":""weatherCode"",""type"":[""null"",""int""],""default"":null},{""name"":""weatherDescription"",""type"":[""null"",""string""],""default"":null},{""name"":""windSpeed"",""type"":[""null"",""double""],""default"":null},{""name"":""precipitation"",""type"":[""null"",""double""],""default"":null}]}},{""name"":""metadata"",""type"":[""null"",{""type"":""map"",""values"":""string""}],""default"":null}]}");
    public string messageId { get; set; } = null!;
    public string subject { get; set; } = null!;
    public string body { get; set; } = null!;
    public string recipient { get; set; } = null!;
    public DateTime timestamp { get; set; }
    public AlertType alertType { get; set; }
    public Severity severity { get; set; }
    public Location location { get; set; } = null!;
    public WeatherConditions weatherConditions { get; set; } = null!;
    public IDictionary<string, string>? metadata { get; set; }
    public virtual Schema Schema => _SCHEMA;
    public virtual object Get(int fieldPos)
    {
        switch (fieldPos)
        {
            case 0: return this.messageId;
            case 1: return this.subject;
            case 2: return this.body;
            case 3: return this.recipient;
            case 4: return this.timestamp;
            case 5: return this.alertType;
            case 6: return this.severity;
            case 7: return this.location;
            case 8: return this.weatherConditions;
            case 9: return this.metadata;
            default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
        };
    }
    public virtual void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: this.messageId = (string)fieldValue; break;
            case 1: this.subject = (string)fieldValue; break;
            case 2: this.body = (string)fieldValue; break;
            case 3: this.recipient = (string)fieldValue; break;
            case 4: this.timestamp = (DateTime)fieldValue; break;
            case 5: this.alertType = (AlertType)fieldValue; break;
            case 6: this.severity = (Severity)fieldValue; break;
            case 7: this.location = (Location)fieldValue; break;
            case 8: this.weatherConditions = (WeatherConditions)fieldValue; break;
            case 9: this.metadata = (IDictionary<string, string>?)fieldValue; break;
            default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
        };
    }
}
