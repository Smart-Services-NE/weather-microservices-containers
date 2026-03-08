# Implementation Plan: End-to-End Integration Tests

This plan outlines the steps to create a comprehensive integration testing suite for the ContainerApp microservices, following the project's TDD-inspired workflow and iDesign principles.

## Phase 1: Test Project Setup and Infrastructure Connection
This phase focuses on creating the new integration test project and verifying it can communicate with the existing Podman-hosted resources (Kafka, Dapr, SQLite).

- [ ] Task: Create `IntegrationTests` project and add necessary dependencies (xUnit, FluentAssertions, Confluent.Kafka, Dapr.Client, EF Core for verification).
- [ ] Task: Configure test settings to target the `podman compose` environment (e.g., Kafka bootstrap servers, Dapr sidecar ports).
- [ ] Task: Implement a simple "Infrastructure Ping" test to verify connectivity to Kafka and the Notification SQLite database.
- [ ] Task: Conductor - User Manual Verification 'Phase 1: Test Project Setup and Infrastructure Connection' (Protocol in workflow.md)

## Phase 2: End-to-End Flow (Freezing Alert Scenario)
In this phase, we implement the primary end-to-end test case for the Freezing Alert flow.

- [ ] Task: Write failing integration test that triggers a `FreezingAlertRequest` in `WeatherService` and asserts that a corresponding notification eventually appears in the `NotificationService` database.
- [ ] Task: Implement the test logic, including triggering the request via `WeatherService`'s API (using Dapr or direct HTTP) and polling the `NotificationService` database for the result.
- [ ] Task: Verify the test passes and that the Avro-serialized data is correctly processed throughout the pipeline.
- [ ] Task: Conductor - User Manual Verification 'Phase 2: End-to-End Flow (Freezing Alert Scenario)' (Protocol in workflow.md)

## Phase 3: Additional Scenarios and Observability Verification
This phase expands the test suite to cover more scenarios and ensures observability metrics are captured.

- [ ] Task: Write tests for additional scenarios (e.g., successful weather subscription updates or other alert types).
- [ ] Task: Verify that integration tests generate traces in Zipkin and metrics in Prometheus during execution.
- [ ] Task: Implement test cleanup logic to ensure each test run starts with a clean or predictable state (e.g., clearing test-specific notifications).
- [ ] Task: Conductor - User Manual Verification 'Phase 3: Additional Scenarios and Observability Verification' (Protocol in workflow.md)

## Phase 4: Finalization and Documentation
Final cleanup, documentation, and verification of the testing process.

- [ ] Task: Update the project's `README.md` or `DOCUMENTATION_GUIDELINES.md` with instructions on how to run the new integration tests.
- [ ] Task: Ensure the `dotnet test` command correctly executes the integration tests alongside existing unit tests.
- [ ] Task: Conductor - User Manual Verification 'Phase 4: Finalization and Documentation' (Protocol in workflow.md)