---
description: Manage OpenTelemetry observability, metrics, and Grafana dashboards
---

# Observability Specialist Workflow

This workflow ensures full visibility into the system's performance and health using OpenTelemetry, Prometheus, and Zipkin.

## 1. Instrumentation Audit
- **Telemetry Registration**: Verify `builder.Services.AddOpenTelemetry()` is correctly configured in `Program.cs`.
- **Activity Sources**: Ensure each service has a unique `ActivitySource` name matching the service name.
- **Trace Propagation**: Check that `DaprClient` and `HttpClient` use the automated instrumentation packages.

## 2. Metric & Trace Lifecycle
- **Metric Definitions**: Verify Managers use `TelemetryUtility` to record `operation.success`, `operation.failure`, and custom domain metrics.
- **Custom Spans**: Ensure long-running operations in Managers are wrapped in explicit `StartActivity` blocks.
- **Tagging Standards**: Enforce mandatory tags like `message.id`, `transaction.id`, or `user.id` on spans for distributed trace correlation.

## 3. Visualization & Alerting
- **Grafana Dashboards**: Update dashboard JSON files to include new service metrics.
- **Scraping Config**: Ensure new `/metrics` endpoints are registered in the Prometheus configuration.
- **Health Verification**: Check that `/health` endpoints are mapped and returning valid JSON for all external dependencies (DBs, Kafka).

## 4. Resilience Monitoring
- **Polly Integration**: Track retry attempts and circuit breaker triggers via OpenTelemetry tags.
- **Latency Analysis**: Use Zipkin to identify bottlenecks in inter-service calls via Dapr.

---

// turbo
// Run this workflow to validate a feature's observability or update monitoring dashboards.
