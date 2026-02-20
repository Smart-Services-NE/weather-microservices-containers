# Design: Freezing Alert Subscription Service

## 1. User Story
"As a user, I want to subscribe to freezing alerts for my zip code so that I am automatically notified whenever the temperature drops below freezing without manual intervention."

## 2. Architectural Design (IDesign)

### A. Contracts Layer
- `ISubscriptionManager`: Orchestrates subscription creation and the batch check process.
- `ISubscriptionAccessor`: Handles persistence of subscriptions (Email, ZipCode).
- `SubscriptionRecord`: DTO for storage.
- `SubscriptionRequest`: DTO for the API endpoint.

### B. Managers Layer
- `SubscriptionManager`:
    - `SubscribeAsync(request)`: Saves subscription via Accessor.
    - `ProcessSubscriptionsAsync()`: Triggered by cron. Fetches all subscriptions, and for each, calls `IWeatherManager.NotifyIfFreezingAsync()`.

### C. Engines Layer
- Existing `WeatherAlertEngine` is sufficient.

### D. Accessors Layer
- `SubscriptionAccessor`: Uses Entity Framework Core with SQLite (or Azure Table Storage via Azurite) to store `SubscriptionRecord`.

### E. Utilities/Components
- **Dapr Cron Binding**: Triggers a POST endpoint in the WebApi every morning (e.g., 6:00 AM) to start the `ProcessSubscriptionsAsync` workflow.

## 3. Data & Messaging
- **Schema**:
    - `Subscriptions` Table: `Id (Guid)`, `Email (string)`, `ZipCode (string)`, `CreatedAt (DateTime)`.
- **Dapr Binding**: `cron-trigger.yaml`
    - Topic: `scheduled-check`
    - Schedule: `@daily` or `0 6 * * *`

## 4. API Endpoints
- `POST /api/weather/subscriptions`: Create a new subscription.
- `POST /api/weather/subscriptions/process`: (Internal/Dapr) Trigger batch processing.

## 5. Observability & Resilience
- **Tracing**: Track the batch process as one parent activity with child spans for each user check.
- **Metrics**: `subscription.created`, `subscription.batch.completed`, `subscription.notifications.triggered`.
- **Polly**: Apply retries to the batch processor to handle transient database or weather API failures during the loop.
