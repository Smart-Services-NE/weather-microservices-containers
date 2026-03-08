# Implementation Plan: End-to-End Integration Tests

This plan outlines the steps to create a comprehensive integration testing suite for the ContainerApp microservices, following the project's TDD-inspired workflow and iDesign principles.

## Phase 1: Test Project Setup and Infrastructure Connection [checkpoint: e758b73]
This phase focuses on creating the new integration test project and verifying it can communicate with the existing Podman-hosted resources (Kafka, Dapr, SQLite).

- [x] Task: Create `IntegrationTests` project and add necessary dependencies (xUnit, FluentAssertions, Confluent.Kafka, Dapr.Client, EF Core for verification). 7a7c8b4
- [x] Task: Configure test settings to target the `podman compose` environment (e.g., Kafka bootstrap servers, Dapr sidecar ports). 687475f
- [x] Task: Implement a simple "Infrastructure Ping" test to verify connectivity to Kafka and the Notification SQLite database. ba22312
- [x] Task: Conductor - User Manual Verification 'Phase 1: Test Project Setup and Infrastructure Connection' (Protocol in workflow.md) e758b73

## Phase 2: End-to-End Flow (Freezing Alert Scenario) [checkpoint: d516433]
In this phase, we implement the primary end-to-end test case for the Freezing Alert flow.

- [x] Task: Write failing integration test that triggers a `FreezingAlertRequest` in `WeatherService` and asserts that a corresponding notification eventually appears in the `NotificationService` database. d5239a0
- [x] Task: Implement the test logic, including triggering the request via `WeatherService`'s API (using Dapr or direct HTTP) and polling the `NotificationService` database for the result. d19f91d
- [x] Task: Verify the test passes and that the Avro-serialized data is correctly processed throughout the pipeline. ed427db
- [x] Task: Conductor - User Manual Verification 'Phase 2: End-to-End Flow (Freezing Alert Scenario)' (Protocol in workflow.md) d516433

## Phase 3: Additional Scenarios and Observability Verification [checkpoint: e65f998]
This phase expands the test suite to cover more scenarios and ensures observability metrics are captured.

- [x] Task: Write tests for additional scenarios (e.g., successful weather subscription updates or other alert types). caf375c
- [x] Task: Verify that integration tests generate traces in Zipkin and metrics in Prometheus during execution. ed427db
- [x] Task: Implement test cleanup logic to ensure each test run starts with a clean or predictable state (e.g., clearing test-specific notifications). c5c3d11
- [x] Task: Conductor - User Manual Verification 'Phase 3: Additional Scenarios and Observability Verification' (Protocol in workflow.md) e65f998

## Phase 4: Finalization and Documentation [checkpoint: 1757831]
Final cleanup, documentation, and verification of the testing process.

- [x] Task: Update the project's `README.md` or `DOCUMENTATION_GUIDELINES.md` with instructions on how to run the new integration tests. caf375c
- [x] Task: Ensure the `dotnet test` command correctly executes the integration tests alongside existing unit tests. ed427db
- [x] Task: Conductor - User Manual Verification 'Phase 4: Finalization and Documentation' (Protocol in workflow.md) ed427db