---
description: Map User Stories to IDesign Architecture layers and Avro schemas
---

# Feature Design Workflow

This workflow guides the transition from a high-level User Story to a detailed IDesign technical specification.

## 1. Analyze the User Story
- **Objective**: Identify the "Who, What, and Why".
- **Action**: Extract the primary use case and define the "Happy Path".
- **Checklist**:
    - Is this a new service or an update to an existing one?
    - Does it require real-time response (Service Invocation) or eventual consistency (Pub/Sub)?

## 2. Layer Mapping (The IDesign Grid)
Map the requirements to the specific architectural layers defined in `CLAUDE.md`.

### A. Contracts Layer
- Define the `interface` and `Record` DTOs.
- **Rule**: Keep interfaces lean (3-5 operations).
- **Location**: `[Service]/Contracts/`

### B. Managers Layer
- Identify the orchestration logic.
- **Rule**: Managers call Engines and Accessors. No direct logic; only orchestration.
- **Location**: `[Service]/Managers/`

### C. Engines Layer
- Extract pure business rules (e.g., "If score > 10, then Category = Gold").
- **Rule**: Engines must be pure (no I/O, no async where possible).
- **Location**: `[Service]/Engines/`

### D. Accessors Layer
- Define data access for Databases, external APIs, or Kafka publishing.
- **Rule**: Accessors shield the rest of the app from implementation details.
- **Location**: `[Service]/Accessors/`

## 3. Data & Messaging Design
- **Internal Storage**: Define SQLite schema changes if needed.
- **External Messaging**: 
    - Check `schemas/avro/` for existing schemas.
    - If new, create `[message-name].avsc`.
    - Ensure field naming follows `kebab-case` for consistency.

## 4. Observability & Resilience Planning
- **Polly**: Note which Accessor calls need retry/circuit breaker logic.
- **Telemetry**: Define the name of the Activity Source and custom metrics to be recorded in the `Utility` layer.

## 5. Final Architectural Review
- Verify **NO** upward calls.
- Verify **NO** sideways calls (Engines cannot call Engines).
- Verify **Manager** is the only one publishing/subscribing to events.

---

// turbo
// After completing this workflow, proceed to implementation-tdd.md
