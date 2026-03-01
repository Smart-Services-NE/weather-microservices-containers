# Design: Weather Sentinel (Custom Alert Thresholds)

## 1. User Story
"As a user, I want to set custom weather alert thresholds (e.g., wind speed > 30mph, temp > 90F) so that I receive notifications tailored to my specific safety and comfort needs."

## 2. Architectural Design (IDesign)

### A. Contracts Layer
- **ThresholdType Enum**:
  ```csharp
  public enum ThresholdType { 
      TemperatureHigh, 
      TemperatureLow, 
      WindSpeed, 
      PrecipitationProbability 
  }
  ```
- **Updated SubscriptionRequest**:
  ```csharp
  public record SubscriptionRequest(
      string Email, 
      string ZipCode, 
      ThresholdType Type, 
      double Value, 
      string ComparisonOperator = "greater-than"
  );
  ```
- **Updated SubscriptionRecord**:
  ```csharp
  public record SubscriptionRecord(
      Guid Id, 
      string Email, 
      string ZipCode, 
      ThresholdType Type, 
      double Value, 
      string ComparisonOperator,
      DateTime CreatedAt
  );
  ```

### B. Managers Layer
- **SubscriptionManager**:
    - `SubscribeAsync(request)`: Saves the specialized subscription.
    - `ProcessSubscriptionsAsync()`: 
        1. Fetch all subscriptions.
        2. Iterate through each subscription.
        3. Fetch current weather for the zip code (leveraging `WeatherManager` and `CacheUtility`).
        4. Use `IWeatherAlertEngine` to evaluate the threshold.
        5. If evaluation is true, publish a tailored alert via `IAlertPublisherAccessor`.

### C. Engines Layer
- **WeatherAlertEngine**:
    - New method: `bool EvaluateThreshold(ThresholdType type, double currentValue, double thresholdValue, string comparisonOperator)`.
    - Contains the logic for comparing weather data against user-defined values.

### D. Accessors Layer
- **SubscriptionAccessor**: 
    - Update EF Core configuration in `WeatherDbContext` to include the new fields.
    - Update `CreateSubscriptionAsync` and `GetAllSubscriptionsAsync` to handle extended data.

## 3. Data & Messaging
- **SQLite Schema (Subscriptions table)**:
    - `Id (Guid)`
    - `Email (string)`
    - `ZipCode (string)`
    - `Type (int)`
    - `Value (double)`
    - `ComparisonOperator (string)`
    - `CreatedAt (DateTime)`
- **Kafka**: Use `weather-alerts` topic with dynamic subjects (e.g., "High Wind Alert for 94105").

## 4. Observability & Resilience
- **Telemetry**: Parent activity `ProcessSentinelsBatch` with child spans for each subscription evaluation.
- **Metrics**: `sentinel.triggered.count`, `sentinel.evaluation.duration`.
- **Polly**: Existing retry policies in `WeatherManager` will protect the weather data fetching part of the loop.

## 5. UI Updates (WeatherWeb)
- Search result page will now have a "Set Sentinel" form instead of just a "Subscribe to Freezing Alerts" button.
- User selects Alert Type, Condition (Greater Than / Less Than), and Value.
