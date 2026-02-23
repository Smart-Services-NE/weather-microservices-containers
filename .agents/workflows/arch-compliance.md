---
description: Verify codebase adherence to strict IDesign architecture principles
---

# Architecture Compliance Workflow

This workflow ensures the codebase maintains the strict IDesign architecture defined in `CLAUDE.md`.

## 1. Structural Audit
- **Namespace Matching**: Ensure project names and namespaces follow the `[ServiceName].[LayerName]` pattern (e.g., `WeatherService.Managers`).
- **File Placement**: Verify components are in the correct physical directory based on their role.
- **Contract Purity**: Ensure the `Contracts` layer contains only interfaces and DTOs (Records). No business logic.

## 2. Dependency Graph Check
- **Closed Architecture**: Verify layers only call down one level (Managers → Engines/Accessors).
- **No Sideways Calls**: Ensure Engines do not call other Engines, and Accessors do not call other Accessors.
- **No Upward Calls**: Verify that lower layers (Accessors, Engines, Utilities) do not reference or call higher layers (Managers, Clients).
- **Static Analysis**: Scan C# project references (`.csproj`) to ensure no illegal dependencies exist between layers.

## 3. Communication Rules
- **Event Ownership**: Verify that ONLY Managers publish or subscribe to events via Dapr or Kafka.
- **Data Encapsulation**: Ensure Accessors are the only components interacting with external resources (DBs, APIs).
- **Service Registration**: Check `ServiceCollectionExtensions.cs` in each layer to ensure the standard registration order and `Scoped` lifetimes are used.

## 4. Constraint Enforcement
- **Lean Interfaces**: Audit service contracts to ensure they have 3-5 operations. Flag any contract with >10 operations for refactoring.
- **Business Logic Placement**: Verify that all non-trivial logic is in `Engines` and orchestration is in `Managers`.

---

// turbo
// Run this workflow before merging any PR or after running `/implementation-tdd`.
