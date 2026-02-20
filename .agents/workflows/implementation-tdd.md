---
description: Implement IDesign components using TDD and Dependency Injection
---

# Implementation & TDD Workflow

This workflow guides the coding phase, ensuring architectural compliance and test-driven quality.

## 1. Project Setup
- Ensure layer-specific projects exist (Contracts, Managers, Engines, Accessors).
- Register new dependencies in the `ServiceCollectionExtensions.cs` of each layer.
- **Rule**: Registration order must be Managers → Engines → Accessors → Utilities.

## 2. Test-Driven Development (Engines First)
- **Step 1**: Create a test class in `[Service].Tests.Engines`.
- **Step 2**: Write a failing test for the business logic.
- **Step 3**: Implement the logic in the `Engine` to pass the test.
- **Step 4**: Refactor for clarity.

## 3. Implementation Sequence
- **Accessors**: Implement external resource calls (Mock these in higher-layer tests).
- **Managers**: Implement the orchestration workflow.
- **WebApi**: Create the Controller/Client endpoints that call the Manager.

## 4. Dependency Injection & Dapr
- Ensure `DaprClient` is injected where needed (usually Managers or specialized Utilities).
- Update `Program.cs` if new middleware or Dapr features are required.

## 5. Local Verification
- Run `dotnet build` to catch interface mismatches.
- Run `dotnet test` and ensure coverage for the new logic is > 80%.

---

// turbo
// After completing this workflow, proceed to obs-resilience.md
