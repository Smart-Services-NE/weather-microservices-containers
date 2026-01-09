# Container App Project Guidelines

This project follows IDesign architecture principles for C#/.NET microservices.

## Project Overview

This is a containerized microservices application consisting of:
- **WeatherService**: Weather forecast API with geocoding and weather data integration
- **WeatherWeb**: Web frontend for weather forecasts (uses Dapr for service invocation)
- **NotificationService**: Kafka-based notification service for email alerts

## Documentation

All project documentation is organized in the `docs/` folder:

- **[Documentation Hub](docs/README.md)** - Main navigation and overview
- **[Documentation Guidelines](docs/DOCUMENTATION_GUIDELINES.md)** - Standards for creating/updating docs

**Documentation Structure**:
```
docs/
├── getting-started/     # Setup and onboarding guides
├── kafka/              # Kafka and messaging documentation
├── services/           # Service-specific documentation
├── infrastructure/     # Infrastructure components
└── scripts/            # Automation and tooling
```

**Key Documentation Principles**:
- **Concise** - Remove redundancy, keep content focused
- **Connected** - Cross-reference related documents
- **Consolidated** - Group related topics together
- **Current** - Keep updated with code changes

When creating or updating documentation, always follow the patterns in [Documentation Guidelines](docs/DOCUMENTATION_GUIDELINES.md).

## Architecture Principles

### IDesign Layered Architecture

All services in this project follow strict IDesign architecture:

#### Component Layers
1. **Contracts** - Pure interfaces and DTOs (no implementation)
2. **Managers** - Orchestrate use cases and workflows
3. **Engines** - Pure business logic (no I/O)
4. **Accessors** - All external resource access (databases, APIs, Kafka, etc.)
5. **Utilities** - Cross-cutting concerns (telemetry, caching, retry policies)
6. **Clients/WebApi** - API entry points

#### Layer Interaction Rules
- **Closed Architecture**: Components can only call down one layer
- **No Sideways Calls**: Except queued calls between Managers
- **No Upward Calls**: Never call up the stack
- **Managers** orchestrate by calling Engines and Accessors
- **Engines** contain pure business logic, can call Accessors for data
- **Accessors** shield external resources from the rest of the system
- **All layers** can call Utilities

#### Event and Messaging Rules
- **Accessors do NOT**:
  - Publish events
  - Subscribe to events
  - Receive queued calls
- **Engines do NOT**:
  - Publish events
  - Subscribe to events
  - Receive queued calls
- **Only Managers**:
  - Can publish events
  - Can subscribe to events
  - Can queue calls to other Managers (one at a time per use case)

## Project Structure

### WeatherService
```
WeatherService/
├── Contracts/           # Interfaces and DTOs
├── Managers/           # WeatherManager (orchestration)
├── Engines/            # WeatherCodeEngine (business logic)
├── Accessors/          # GeoCodingAccessor, WeatherDataAccessor
├── Utilities/          # CacheUtility, TelemetryUtility
├── Clients/WebApi/     # REST API endpoints
└── Tests/              # Unit tests for all layers
```

**Key Classes**:
- `WeatherManager`: Orchestrates weather forecast retrieval workflow
- `WeatherCodeEngine`: Translates weather codes to human-readable descriptions
- `GeoCodingAccessor`: Calls Zippopotam API for location data
- `WeatherDataAccessor`: Calls Open-Meteo API for weather data
- `CacheUtility`: HybridCache integration (5-minute TTL)
- `TelemetryUtility`: OpenTelemetry with Zipkin and Prometheus

### NotificationService
```
NotificationService/
├── Contracts/          # Interfaces and DTOs
├── Managers/          # NotificationManager (orchestration)
├── Engines/           # NotificationEngine (message parsing, validation)
├── Accessors/         # EmailAccessor, NotificationStorageAccessor (NO Kafka!)
├── Utilities/         # TelemetryUtility, RetryPolicyUtility, KafkaConsumerUtility
├── Clients/WebApi/    # Background service + API endpoints
└── Tests/             # Unit tests for all layers
```

**Key Classes**:
- `NotificationManager`: Orchestrates message validation → email sending → persistence
- `NotificationEngine`: Validates messages and builds email requests (parsing moved to utility)
- `KafkaConsumerUtility`: Kafka consumer utility (not an Accessor - utilities can be called by any layer)
- `EmailAccessor`: SMTP email sending
- `NotificationStorageAccessor`: SQLite persistence with EF Core
- `RetryPolicyUtility`: Exponential backoff retry logic (Polly)
- `KafkaBackgroundService`: Continuous Kafka topic subscription (calls utility directly)

## Technology Stack

### Infrastructure
- **Docker Compose**: Orchestrates all services
- **Dapr**: Service invocation and resiliency (sidecars for each service)
- **Kafka**: Message streaming (Confluent with Zookeeper)
- **Zipkin**: Distributed tracing
- **Prometheus**: Metrics collection
- **Grafana**: Metrics visualization

### .NET Stack
- **.NET 10.0**: All services target net10.0
- **OpenTelemetry**: Observability (Zipkin + Prometheus exporters)
- **HybridCache**: Microsoft's new caching library (WeatherService)
- **Entity Framework Core**: ORM for NotificationService (SQLite)
- **Confluent.Kafka**: Kafka client library
- **Polly**: Resilience and retry policies

### Testing
- **xUnit**: Test framework
- **Moq**: Mocking library
- **FluentAssertions**: Assertion library

## Service Configuration

### WeatherService
- **Port**: 8080 (Dapr HTTP: 3501, Dapr gRPC: 50001)
- **Endpoints**:
  - `GET /api/weather/forecast?zipcode={zipCode}`
  - `GET /api/weather/validate?zipcode={zipCode}`
  - `GET /api/weather/cached?zipcode={zipCode}`
  - `GET /metrics` (Prometheus)
- **External APIs**:
  - Geocoding: `http://api.zippopotam.us/us/{zipCode}`
  - Weather: `https://api.open-meteo.com/v1/forecast`

### NotificationService
- **Port**: 8082 (Dapr HTTP: 3502, Dapr gRPC: 50003)
- **Kafka Topics**: `weather-alerts`, `general-events`
- **Endpoints**:
  - `GET /health` (SQLite health check)
  - `GET /metrics` (Prometheus)
  - `GET /api/notifications/pending`
  - `POST /api/notifications/{id}/retry`
- **Database**: SQLite (`notifications.db`)
- **Email**: SMTP (configurable in appsettings.json)

### Kafka Message Schemas

The project includes Avro schemas for Kafka message standardization:

**Location**: [schemas/avro/](schemas/avro/)

**Available Schemas**:
1. **notification-message.avsc**: Generic schema for both `weather-alerts` and `general-events` topics
   - Fields: messageId, subject, body, recipient, timestamp, metadata (map)
   - Compatible with NotificationService's NotificationMessage DTO
   - Recommended for simple email notifications

2. **weather-alert.avsc**: Rich schema for weather-specific alerts
   - Extends notification-message with structured weather data
   - Includes: alertType (enum), severity (enum), location (record), weatherConditions (record)
   - Supports detailed weather information (temperature, weatherCode, wind, precipitation)

**JSON Message Format** (for kafka-console-producer):
```json
{
  "messageId": "unique-id",
  "subject": "Email subject",
  "body": "Email body content",
  "recipient": "user@example.com",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "custom@sender.com",
    "isHtml": "true"
  }
}
```

**Documentation**: See [schemas/README.md](schemas/README.md) for:
- Complete field definitions
- JSON examples for all message types
- Python and C# producer examples
- kafka-console-producer commands
- Schema Registry setup instructions
- Testing procedures

## Development Guidelines

### Service Contract Design
- Strive for 3-5 operations per service contract
- Avoid contracts with a single operation
- Reject service contracts with 20+ operations
- Avoid property-like operations
- Limit to 1-2 contracts per service

### Testing Requirements
- **Always create tests** for new features
- Test all layers: Managers, Engines, Accessors
- Use Moq for mocking dependencies
- Ensure new functionality doesn't break existing tests
- Test projects follow same structure as source projects

### Dependency Injection Pattern
Each layer provides a `ServiceCollectionExtensions.cs` with extension methods:
```csharp
public static IServiceCollection AddServiceNameManagers(this IServiceCollection services)
{
    services.AddScoped<IServiceManager, ServiceManager>();
    services.AddServiceNameEngines();
    services.AddServiceNameAccessors();
    services.AddServiceNameUtilities();
    return services;
}
```

**Registration Order**: Managers → Engines → Accessors → Utilities

**Lifetime**: All services use `Scoped` lifetime (request-scoped)

### Error Handling
- Use `ErrorInfo` record for standardized errors: `record ErrorInfo(string Code, string Message)`
- Use Result pattern: `record SomeResult(bool Success, Data? Data = null, ErrorInfo? Error = null)`
- Log errors with structured logging (Microsoft.Extensions.Logging)
- Telemetry: Track failures with metrics via TelemetryUtility

### Telemetry Standards
- **ActivitySource Name**: Use service name (e.g., "WeatherService", "NotificationService")
- **Set Tags**: `telemetry.SetTag("key", value)` for important context
- **Record Metrics**: `telemetry.RecordMetric("metric.name", value)` for counters
- **Start Activities**: `using var activity = telemetry.StartActivity("OperationName")`
- **Zipkin Endpoint**: `http://zipkin:9411/api/v2/spans`
- **Prometheus**: Exposed via `/metrics` endpoint

### Retry and Resilience
- Use `RetryPolicyUtility` for operations that may fail transiently
- Exponential backoff: 2s → 4s → 8s → 16s → 5min (max)
- Max 5 retry attempts
- Use Polly ResiliencePipeline with jitter
- Manual Kafka offset commit (only after successful processing)

## Docker and Deployment

### Running the Application
```bash
# Start all services
docker-compose up -d

# View logs for specific service
docker-compose logs -f notification-api

# View logs for Dapr sidecar
docker-compose logs -f notification-api-dapr

# Check service health
curl http://localhost:8082/health

# Stop all services
docker-compose down

# Stop and remove volumes (clean state)
docker-compose down -v
```

### NotificationService Docker Configuration
- **Image**: Built from multi-stage Dockerfile (SDK 10.0 → Runtime 10.0)
- **Port**: 8082 (maps to container port 8080)
- **Health Check**: HTTP GET to `/health` endpoint every 30s
- **Database**: SQLite persisted in Docker volume (`./notification-data:/app/data`)
- **Dapr Integration**:
  - App protocol: HTTP (for Dapr-to-app communication)
  - gRPC port 50003 for service invocation
  - Cloud Events support enabled
  - Pub/Sub subscribe handler registered
  - Depends on healthy app container before starting

### Service Ports
- WeatherWeb: 8081
- WeatherService API: 8080
- NotificationService API: 8082
- Kafka: 9092 (host), 9093 (container)
- Zookeeper: 2181
- Zipkin: 9411
- Prometheus: 9090
- Grafana: 3000

### Dapr Sidecars
- weather-api: HTTP 3501, gRPC 50001, Protocol: HTTP
- weather-web: HTTP 3500, gRPC 50002, Protocol: HTTP
- notification-api: HTTP 3502, gRPC 50003, Protocol: HTTP
  - Dapr-to-app communication: HTTP on port 8080
  - Dapr service invocation: gRPC on port 50003 (inter-service communication)
  - Health checks enabled with 30s interval
  - Metrics exposed on Dapr metrics port

## Common Patterns

### Manager Pattern (Orchestration)
```csharp
public async Task<Result> ProcessAsync(Request request, CancellationToken ct)
{
    using var activity = _telemetry.StartActivity("ProcessOperation");

    // 1. Validate via Engine
    if (!_engine.Validate(request))
        return new Result(false, Error: new ErrorInfo("INVALID", "Validation failed"));

    // 2. Call Accessor for external data
    var data = await _accessor.GetDataAsync(request, ct);

    // 3. Process via Engine
    var processed = _engine.Process(data);

    // 4. Store via Accessor
    await _storageAccessor.SaveAsync(processed, ct);

    _telemetry.RecordMetric("operation.success", 1);
    return new Result(true, Data: processed);
}
```

### Engine Pattern (Pure Logic)
```csharp
public ProcessedData Process(RawData data)
{
    // Pure business logic - no I/O, no async
    var result = ApplyBusinessRules(data);
    return result;
}
```

### Accessor Pattern (External Resources)
```csharp
public async Task<Data> GetDataAsync(Request request, CancellationToken ct)
{
    try
    {
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Data>(ct);
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Failed to fetch data");
        throw;
    }
}
```

## Important Notes

### Kafka Integration
- **Architecture Compliance**: Kafka consumption is handled by `KafkaConsumerUtility` (Utilities layer), NOT an Accessor
  - Accessors cannot subscribe to events/messages per IDesign rules
  - Utilities are cross-cutting concerns and can be called by any layer
- **Background Service**: `KafkaBackgroundService` in Clients layer calls the utility directly
- **Manual Offset Management**: Only commit after successful processing
- **Idempotency**: Manager checks for duplicate `messageId` before processing
- **Message Parsing**: Done by utility before passing to Manager
- **Manager Receives Parsed Message**: Manager receives `NotificationMessage` object, not raw JSON

### Database Migrations
- NotificationService uses SQLite with `EnsureCreated()` on startup
- For production, replace with proper migrations
- Database file stored in `/app/data` (Docker volume mounted)

### Email Configuration
- Configure SMTP settings in `appsettings.json`
- For Gmail: Use App Password (not regular password)
- For development: Consider using MailHog or similar SMTP test server

## Troubleshooting

### Build Issues
- Ensure all projects target `net10.0`
- Check package version compatibility
- Use `Microsoft.Extensions.DependencyInjection.Abstractions` version `10.0.1`

### Kafka Connection Issues
- Verify Kafka is running: `docker-compose logs kafka`
- Check bootstrap servers: Inside containers use `kafka:9093`, localhost use `localhost:9092`
- Verify topics exist: `kafka-topics --list --bootstrap-server localhost:9092`

### Dapr Issues
- Check Dapr sidecars are running: `docker-compose ps`
- Verify Dapr placement service is healthy
- Check Dapr logs: `docker-compose logs weather-api-dapr`

## Adding a New Service

**CRITICAL REQUIREMENT**: All new services MUST be containerized using Docker and integrated with Dapr using gRPC for service invocation.

When adding a new microservice to this project:

### 1. Project Structure (IDesign Architecture)
Create the following layered structure:
```
ServiceName/
├── Contracts/          # Interfaces and DTOs
├── Engines/           # Business logic
├── Accessors/         # External resource access
├── Managers/          # Orchestration
├── Utilities/         # Cross-cutting concerns
├── Clients/WebApi/    # API endpoints
└── Tests/             # Unit tests
```

### 2. Naming Convention
- **Projects**: `ServiceName.LayerName` (e.g., `OrderService.Managers`)
- **Solution**: `dotnet sln add [ProjectPath]`
- **Namespaces**: Match project names

### 3. Dependency Injection Setup
Create `ServiceCollectionExtensions.cs` in each layer:
```csharp
public static IServiceCollection AddServiceNameManagers(this IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IServiceManager, ServiceManager>();
    services.AddServiceNameEngines();
    services.AddServiceNameAccessors(configuration);
    services.AddServiceNameUtilities(configuration);
    return services;
}
```

### 4. Containerization (MANDATORY)

#### A. Create Dockerfile
**Location**: `ServiceName/Clients/WebApi/Dockerfile`

**Template**:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["ServiceName/Clients/WebApi/ServiceName.Api.csproj", "ServiceName/Clients/WebApi/"]
COPY ["ServiceName/Managers/ServiceName.Managers.csproj", "ServiceName/Managers/"]
COPY ["ServiceName/Engines/ServiceName.Engines.csproj", "ServiceName/Engines/"]
COPY ["ServiceName/Accessors/ServiceName.Accessors.csproj", "ServiceName/Accessors/"]
COPY ["ServiceName/Utilities/ServiceName.Utilities.csproj", "ServiceName/Utilities/"]
COPY ["ServiceName/Contracts/ServiceName.Contracts.csproj", "ServiceName/Contracts/"]

# Restore dependencies
RUN dotnet restore "ServiceName/Clients/WebApi/ServiceName.Api.csproj"

# Copy source code
COPY . .
WORKDIR "/src/ServiceName/Clients/WebApi"

# Build
RUN dotnet build "ServiceName.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "ServiceName.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ServiceName.Api.dll"]
```

#### B. Add to docker-compose.yml
Add BOTH the service container AND its Dapr sidecar:

```yaml
# Service Container
service-name-api:
  build:
    context: .
    dockerfile: ServiceName/Clients/WebApi/Dockerfile
  container_name: containerapp-service-name-api-1
  ports:
    - "80XX:8080"  # Use next available port (8083, 8084, etc.)
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ASPNETCORE_URLS=http://+:8080
    - ConnectionStrings__ServiceDb=Data Source=/app/data/service.db
    - Zipkin__Endpoint=http://zipkin:9411/api/v2/spans
    # Add service-specific environment variables
  volumes:
    - ./service-data:/app/data  # For data persistence
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
    interval: 30s
    timeout: 10s
    retries: 3
    start_period: 40s
  networks:
    - containerapp-network
  depends_on:
    kafka:
      condition: service_healthy
    # Add other dependencies

# Dapr Sidecar (MANDATORY for all services)
service-name-api-dapr:
  image: "daprio/daprd:latest"
  command:
    - "./daprd"
    - "-app-id"
    - "service-name-api"
    - "-app-port"
    - "8080"
    - "-app-protocol"
    - "http"
    - "-dapr-http-port"
    - "35XX"  # Use next available port (3503, 3504, etc.)
    - "-dapr-grpc-port"
    - "500X"  # Use next available port (50004, 50005, etc.)
    - "-placement-host-address"
    - "dapr-placement:50006"
    - "-components-path"
    - "/components"
    - "-config"
    - "/configuration/config.yaml"
  volumes:
    - "./dapr/components:/components"
    - "./dapr/configuration:/configuration"
  depends_on:
    service-name-api:
      condition: service_healthy
    dapr-placement:
      condition: service_started
  network_mode: "service:service-name-api"
```

### 5. Dapr Integration in Program.cs (MANDATORY)

**Add these to Program.cs**:
```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// MANDATORY: Add Dapr client
builder.Services.AddDaprClient();

// Add your service layers
builder.Services.AddServiceNameManagers(builder.Configuration);

// MANDATORY: Add Dapr to controllers
builder.Services.AddControllers().AddDapr();

// MANDATORY: Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("ServiceName"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("ServiceName")
        .AddZipkinExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["Zipkin:Endpoint"]
                ?? "http://zipkin:9411/api/v2/spans");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// MANDATORY: Add health checks
builder.Services.AddHealthChecks()
    .AddSqlite(builder.Configuration.GetConnectionString("ServiceDb") ?? "Data Source=service.db");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// MANDATORY: Enable Dapr Cloud Events
app.UseCloudEvents();

// MANDATORY: Map Prometheus endpoint
app.MapPrometheusScrapingEndpoint();

// MANDATORY: Map health checks
app.MapHealthChecks("/health");

// MANDATORY: Map Dapr subscribe handler
app.MapSubscribeHandler();

// Map controllers
app.MapControllers();

app.Run();
```

### 6. Service Communication via Dapr

**Call other services using DaprClient**:
```csharp
public class SomeManager : ISomeManager
{
    private readonly DaprClient _daprClient;

    public SomeManager(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    public async Task<Result> CallOtherServiceAsync(Request request, CancellationToken ct)
    {
        try
        {
            // Invoke another service via Dapr gRPC
            var response = await _daprClient.InvokeMethodAsync<Request, Response>(
                HttpMethod.Post,
                "other-service-api",  // Dapr app-id
                "/api/endpoint",
                request,
                ct);

            return new Result(true, Data: response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke other service");
            return new Result(false, Error: new ErrorInfo("INVOCATION_FAILED", ex.Message));
        }
    }
}
```

### 7. Testing Requirements
- Create test projects for each layer: `ServiceName.Tests.Managers`, `ServiceName.Tests.Engines`, etc.
- Use xUnit, Moq, and FluentAssertions
- Ensure new functionality doesn't break existing tests
- Test Dapr integration using `DaprClient` mocks

### 8. Required NuGet Packages

**Clients/WebApi Project**:
```xml
<PackageReference Include="Dapr.AspNetCore" Version="1.14.0" />
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.10.0" />
<PackageReference Include="OpenTelemetry.Exporter.Zipkin" Version="1.10.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.10.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.10.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.10.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.3.0" />
<PackageReference Include="AspNetCore.HealthChecks.Sqlite" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.1" />
```

### 9. Port Assignment

Assign sequential ports for each new service:
- **Service API**: 8080 + N (e.g., 8083, 8084...)
- **Dapr HTTP**: 3500 + N (e.g., 3503, 3504...)
- **Dapr gRPC**: 50001 + N (e.g., 50004, 50005...)

### 10. Documentation
Update this CLAUDE.md with:
- Service overview and purpose
- Project structure
- Key classes and their responsibilities
- Configuration settings
- API endpoints
- External dependencies
- Port assignments

## Service Containerization Checklist

Before considering a new service complete, verify:

- [ ] Dockerfile created with multi-stage build (SDK → Runtime)
- [ ] Service added to docker-compose.yml with health checks
- [ ] Dapr sidecar configured in docker-compose.yml
- [ ] gRPC port assigned for service invocation
- [ ] Program.cs includes `AddDaprClient()` and `.AddDapr()`
- [ ] Cloud Events enabled with `UseCloudEvents()`
- [ ] OpenTelemetry configured (Zipkin + Prometheus)
- [ ] Health check endpoint mapped at `/health`
- [ ] Prometheus metrics endpoint mapped
- [ ] Service communication uses DaprClient for inter-service calls
- [ ] Environment variables configured in docker-compose.yml
- [ ] Data volumes mounted for persistence (if needed)
- [ ] Service depends on required infrastructure (Kafka, databases, etc.)
- [ ] CLAUDE.md updated with service details and port assignments

## References

- IDesign Architecture: Follow global CLAUDE.md guidelines
- OpenTelemetry: https://opentelemetry.io/docs/instrumentation/net/
- Dapr: https://docs.dapr.io/
- Confluent Kafka: https://docs.confluent.io/kafka-clients/dotnet/current/overview.html
