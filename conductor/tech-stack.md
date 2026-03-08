# Tech Stack: ContainerApp

## Runtime and Languages
- **.NET 10.0:** The primary runtime for all microservices.
- **C#:** The primary programming language.

## Web and API Frameworks
- **ASP.NET Core:** Web framework for both Razor Pages and Web APIs.
- **Razor Pages:** Frontend framework for the WeatherWeb application.
- **Dapr (Distributed Application Runtime):** Facilitates service-to-service communication, state management, and event-driven architectures.

## Messaging and Events
- **Apache Kafka (Confluent):** High-throughput message broker for asynchronous communication between services.
- **Avro:** Binary serialization format used for Kafka messages to ensure schema evolution and data integrity.

## Persistence and Data Access
- **SQLite:** Lightweight relational database for local persistence of notifications and weather data.
- **Entity Framework Core (EF Core):** Object-Relational Mapper (ORM) for data access and management.

## Observability and Monitoring
- **OpenTelemetry:** Standardized instrumentation for traces and metrics.
- **Zipkin:** Distributed tracing system to visualize service interactions and performance.
- **Prometheus:** Metrics collection and alerting system.
- **Grafana:** Visualization platform for metrics and traces.

## Resiliency and Performance
- **Polly:** Library for implementing transient-fault-handling policies like retries and circuit breakers.
- **HybridCache:** High-performance caching mechanism provided by .NET 10.

## Infrastructure and Tooling
- **Podman (Compose):** Containerization and orchestration for local development and testing.
- **xUnit:** Primary testing framework for unit and integration tests.
- **FluentAssertions:** Assertion library for readable and expressive tests.
- **Moq:** Mocking framework for unit testing.
