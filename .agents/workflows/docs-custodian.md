---
description: Maintain documentation sync between code, architecture, and guidelines
---

# Documentation Custodian Workflow

This workflow ensures the project's documentation remains the single source of truth for the evolving system.

## 1. Guideline Synchronization
- **CLAUDE.md Updates**: Update the "Services Overview", "Service Ports", and "Key Classes" whenever new features or services are added.
- **Documentation Hub**: Ensure `docs/README.md` correctly indexes new service-level documentation.

## 2. Technical Documentation
- **Service Specs**: Generate or update `docs/services/[service-name].md` with:
    - API Endpoints
    - Environment Variables
    - Kafka Topics
    - External Dependencies
- **Infrastructure Docs**: Update `docs/infrastructure/` when Dapr components or Podman configurations change.

## 3. Standard Enforcement
- **Consistency Check**: Verify that naming conventions and architectural terms used in code (e.g., "Accessor") match the definitions in the documentation.
- **Link Auditing**: Fix any broken internal documentation links in Markdown files.

## 4. Onboarding Maintenance
- **Quick Start Sync**: Update `docs/getting-started/quick-start.md` if the startup sequence or prerequisite tooling changes.
- **Changelog Management**: Ensure high-level architectural changes are summarized in the project root README.

---

// turbo
// Run this workflow as the final step of any feature development or architectural change.
