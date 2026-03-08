# Specification: End-to-End Integration Tests

## Overview
This track involves creating a comprehensive suite of integration tests that verify the full data flow across all resources in the ContainerApp microservices ecosystem. These tests will ensure that the interactions between `WeatherWeb`, `WeatherService`, and `NotificationService`, mediated by Dapr and Kafka, are functioning correctly in a live-like environment.

## Functional Requirements
1. **End-to-End Flow Verification:** 
   - Verify that a weather alert request from `WeatherWeb` or `WeatherService` correctly propagates through Kafka.
   - Verify that `NotificationService` consumes the message and accurately processes it (e.g., updates SQLite, "sends" an email/log).
2. **Scenario Coverage:** 
   - **Freezing Alert Flow:** Trigger a freezing alert and verify the notification is stored in the database.
   - **Subscription Flow:** (If applicable) Verify that weather subscription changes are reflected in the notification behavior.
3. **Data Integrity:** 
   - Ensure Avro schema serialization/deserialization works correctly across the Kafka pipeline.
   - Validate that data persisted in `NotificationService` matches the original alert data.

## Non-Functional Requirements
1. **Environment Consistency:** Tests should run against the existing `podman compose` infrastructure (Kafka, Dapr sidecars, SQLite).
2. **Observability:** Integration tests should ideally generate traces that can be viewed in Zipkin/Grafana during the test run.
3. **Clean State:** Tests should attempt to leave the system in a known state or handle data cleanup to avoid side effects between runs.

## Acceptance Criteria
- [ ] A new integration test project/module exists.
- [ ] Tests successfully trigger a "Freezing Alert" in `WeatherService` and verify its receipt in `NotificationService`.
- [ ] All tests pass when the `podman compose` environment is active.
- [ ] The testing suite can be executed via a single command (e.g., `dotnet test IntegrationTests`).

## Out of Scope
- Unit testing individual components (covered in existing unit tests).
- Performance/Load testing.
- UI/Frontend testing (Selenium/Playwright) for `WeatherWeb` (unless required for the trigger).