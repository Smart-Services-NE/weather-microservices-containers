---
description: Orchestrate all specialized agents to deliver a feature from User Story to PR
---

# Feature Orchestrator Workflow

This workflow coordinates the entire agent team to ensure a safe, compliant, and fully documented feature implementation. Use this for any new User Story or significant feature request.

## Phase 1: Blueprinting
1. **Design**: Run `/feature-design` to map the story to IDesign layers and identify necessary components.
// turbo
2. **Infrastructure**: Run `/devops-infra` to prepare `Containerfile` and `compose.yaml` changes, and `/messaging-schema` if the feature involves Kafka events.

## Phase 2: Implementation
// turbo
3. **Development**: Run `/implementation-tdd` to build the feature using Test-Driven Development.
// turbo
4. **Reliability**: Run `/obs-resilience` to integrate OpenTelemetry tracing and Polly resilience policies into the new code.

## Phase 3: Validation (The Quality Gates)
// turbo
5. **Architectural Audit**: Run `/arch-compliance` to ensure the implementation strictly adheres to IDesign layering and communication rules.
// turbo
6. **Observability Sync**: Run `/obs-specialist` to verify metrics are being recorded and to update Grafana dashboards if needed.
// turbo
7. **Security Check**: Run `/security-guardian` to scan for hardcoded secrets and verify the security posture of new configurations.

## Phase 4: Finalization
// turbo
8. **Documentation**: Run `/docs-custodian` to sync `CLAUDE.md`, service documentation, and the documentation hub.
// turbo
9. **Delivery**: Run `/create-pr` to finalize the branch and prepare the Pull Request for human review.

---

// turbo-all
// Execute this orchestrator when a new feature request is received.
