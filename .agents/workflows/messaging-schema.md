---
description: Manage Kafka message streaming, Avro schemas, and service-to-service events
---

# Messaging & Schema Workflow

This workflow governs the Kafka ecosystem and ensures data consistency across microservices.

## 1. Schema Lifecycle
- **Definition**: Manage `.avsc` files in `schemas/avro/`.
- **Naming Conventions**: Use `kebab-case` for file names and `PascalCase` for record names.
- **Compatibility**: Perform "dry-run" checks for schema evolution (Full, Forward, or Backward compatibility).

## 2. Topic Management
- **Topic Registration**: Ensure new topics are documented in `CLAUDE.md` and created in the `compose.yaml` (if using local Kafka) or environment config.
- **Schema Mapping**: Map each Kafka topic to its corresponding Avro schema.

## 3. Implementation Audit
- **Utility Selection**: Ensure Kafka consumption uses `KafkaConsumerUtility` and publishing uses `KafkaPublisherUtility`.
- **Deserialization**: Verify that the `KafkaBackgroundService` correctly identifies and processes both JSON and Avro formats.
- **Idempotency**: Confirm that Managers handle the `messageId` field to prevent duplicate processing.

## 4. Documentation & Tools
- **Payload Samples**: Generate or update `test-payloads/` with valid JSON/Avro examples for new message types.
- **Producer Scripts**: Update Python scripts in `scripts/` to support new message schemas for end-to-end testing.

---

// turbo
// Run this workflow when adding new events, modifying schemas, or debugging Kafka connections.
