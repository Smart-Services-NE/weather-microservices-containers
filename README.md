# containerApp

A cloud-native .NET 10 microservices app with Dapr, HybridCache, and full observability.

## 🏗️ Architecture

-   **Frontend**: `WeatherWeb` (Razor Pages)
-   **Backend**: `WeatherService` (Minimal API)
-   **Sidecars**: Dapr (`daprd`) for service invocation
-   **Observability**:
    -   **Zipkin**: Distributed tracing (Port 9411)
    -   **Prometheus**: Metrics collection (Port 9090)
    -   **Grafana**: Visualization dashboard (Port 3000)
-   **Caching**: .NET HybridCache with stampede protection

## 🚀 Getting Started

### Run
```bash
docker compose up -d --build
```

### Access
-   **Frontend**: [http://localhost:8081](http://localhost:8081)
-   **Grafana**: [http://localhost:3000](http://localhost:3000) (admin/admin)
-   **Prometheus**: [http://localhost:9090](http://localhost:9090)
-   **Zipkin**: [http://localhost:9411](http://localhost:9411)
-   **Dapr Dashboard**: [http://localhost:9999](http://localhost:9999) (if running dashboard)

## 🛠️ Features

-   **Service Invocation**: Frontend calls Backend via Dapr sidecar.
-   **HybridCache**: Backend caches external API responses (5min TTL).
-   **Custom Tracing**: Spans tagged with `weather.source` ("cache" vs "upstream").
-   **Metrics**: Application metrics exposed at `/metrics` and visualized in Grafana.
