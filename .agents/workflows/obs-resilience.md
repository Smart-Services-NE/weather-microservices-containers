---
description: Add OpenTelemetry observability and Polly resilience to the service
---

# Observability & Resilience Workflow

This workflow ensures the service is production-ready, durable, and visible in dashboards.

## 1. Structured Telemetry (OpenTelemetry)
- **Tracing**:
    - Ensure `ActivitySource` is defined in the `Utilities` layer.
    - Wrap critical operations in `Managers` and `Accessors` with `using var activity = _telemetry.StartActivity("OperationName")`.
    - Tag activities with meaningful data: `activity?.SetTag("entity.id", id)`.
- **Metrics**:
    - Identify key performance indicators (KPIs) like `operation.success` or `request.duration`.
    - Record metrics using `_telemetry.RecordMetric("metric_name", value)`.
- **Verification**: Check http://localhost:9411 (Zipkin) for traces and http://localhost:8080/metrics (or appropriate port) for Prometheus data.

## 2. Resilience Policies (Polly)
- **Identification**: Find "brittle" operations (external API calls, database writes, Kafka publishing).
- **Implementation**:
    - Use `RetryPolicyUtility` for transient failures.
    - Default policy: Exponential backoff (2s → 4s → 8s) with jitter.
    - Apply policies in **Accessors** to shield the system from external failure.
- **Circuit Breakers**: For high-volume services, implement a circuit breaker to prevent cascading failures.

## 3. Health & Readiness
- **Endpoint**: Verify `/health` returns `200 OK`.
- **Dependencies**: Ensure the health check provider includes new dependencies (e.g., `.AddSqlite()` or `.AddUrlGroup()` for external APIs).
- **Dapr Integration**: Ensure the `compose.yaml` health check matches the app's internal health check interval.

## 4. Dapr Sidecar Optimization
- Check `config.yaml` for resiliency policies if using Dapr's built-in resilience.
- Verify that the Dapr sidecar is correctly collecting metrics (check Dapr logs).

## 5. Verification in Grafana
- **Step 1**: Ensure all services are up: `podman compose up -d`.
- **Step 2**: Trigger the feature and watch the Grafana dashboard (http://localhost:3000).
- **Step 3**: Verify the "Error Rate" and "Latency" panels reflect the new service.

---

// turbo
// After completion, the feature is ready for final PR and Release phase, proceed to create-pr.md