# Feature Design: Freezing Temperature Alert

**User Story**: "As a user I want to be notified when the temperature drops below freezing."

## 1. Architectural Mapping (IDesign)

### Contracts Layer
- **IWeatherAlertEngine**: Pure logic to determine if a temperature is freezing.
- **IAlertPublisherAccessor**: Publishes alerts to Kafka.
- **IWeatherManager (Update)**: Orchestrates the location lookup, weather fetch, freezing check, and alert publication.
- **WeatherAlert DTO**: Data structure for the alert message.

### Managers Layer
- **WeatherManager**:
  - `CheckAndNotifyFreezingAsync(zipCode, email)`:
    1. Get coordinates via `GeoCodingAccessor`.
    2. Get forecast via `WeatherDataAccessor`.
    3. Check freezing via `WeatherAlertEngine`.
    4. If freezing, publish via `AlertPublisherAccessor`.

### Engines Layer
- **WeatherAlertEngine**: 
  - `IsFreezing(double celsius)`: Returns `true` if `celsius <= 0`.

### Accessors Layer
- **AlertPublisherAccessor**: 
  - Handles Kafka connection.
  - Publishes to `weather-alerts` topic using Avro schema.

## 2. Messaging Design
- **Topic**: `weather-alerts`
- **Schema**: `schemas/avro/weather-alert.avsc`
- **Mechanism**: Event-driven (Pub/Sub) via Kafka.

## 3. Observability & Resilience
- **Metrics**: Count of `freezing_alerts_published`.
- **Tracing**: Follow trace context from API request through Kafka publication.
- **Resilience**: Exponential backoff retry for Kafka publication.

## 4. Proposed API Change
- `POST /api/weather/alerts/freezing`
- Body: `{ "zipCode": "90210", "email": "user@example.com" }`

---
**Next Step**: Run `[/implementation-tdd]` to begin building the components.
