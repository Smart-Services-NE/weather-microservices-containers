# Project Overview

This is a cloud-native .NET 10 microservices application designed to provide weather forecasts and send notifications. It utilizes a full observability stack with Dapr, Kafka, and other modern technologies. The architecture follows the IDesign principles, emphasizing a layered approach with clear separation of concerns.

**Key Technologies:**

*   **.NET 10:** The latest version of the .NET framework.
*   **Dapr:** Used for service invocation and resiliency.
*   **Kafka:** For asynchronous messaging between services.
*   **Podman:** For containerization and orchestration.
*   **OpenTelemetry, Zipkin, Prometheus, Grafana:** For a comprehensive observability stack.
*   **IDesign Architecture:** A layered architecture with volatility-based decomposition.

**Services:**

*   **WeatherWeb:** A Razor Pages frontend for displaying weather information.
*   **WeatherService:** A microservice that provides weather forecasts.
*   **NotificationService:** A microservice that consumes Kafka messages and sends email notifications.

# Building and Running

**Prerequisites:**

*   .NET 10 SDK
*   Podman

**Commands:**

*   **Start all services:**
    ```bash
    podman compose up -d --build
    ```
*   **Stop all services:**
    ```bash
    podman compose down
    ```
*   **Run tests:**
    ```bash
    dotnet test
    ```

# Development Conventions

*   **Architecture:** The project follows the IDesign architecture, which consists of the following layers:
    *   **Contracts:** Interfaces and data transfer objects (DTOs).
    *   **Managers:** Orchestration of business logic.
    *   **Engines:** Implementation of business logic.
    *   **Accessors:** Data access and external service communication.
    *   **Utilities:** Cross-cutting concerns like caching and telemetry.
*   **Testing:** The project has a suite of unit tests that can be run with the `dotnet test` command.
*   **Configuration:** Environment variables are used for configuration. A `.env.example` file is provided as a template.
*   **Containerization:** All services are containerized using Podman and defined in the `compose.yaml` file.
