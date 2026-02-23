---
description: Manage secrets, secure configurations, and dependency integrity
---

# Security & Secret Guardian Workflow

This workflow protects the system from credential leakage and ensures a secure posture for microservices.

## 1. Secret Auditing
- **Scan for Leaks**: Check for hardcoded API keys, passwords, or connection strings in code, `appsettings.json`, and environment variables.
- **File Integrity**: Ensure sensitive files (e.g., `api-key-*.txt`, `*.pfx`) are ignored by Git or stored in secure volumes.

## 2. Configuration Hardening
- **Secret Store Migration**: Propose moving plain-text secrets to environment variables, `.env` files (excluded from Git), or Dapr Secret Stores.
- **Volume Permissions**: Verify that sensitive data volumes in `compose.yaml` have appropriate persistence and access controls.

## 3. Dependency & Supply Chain
- **Package Scanning**: Check for vulnerable NuGet packages and recommend updates to stable, patched versions.
- **Service Identity**: Ensure Dapr `app-id`s are used for inter-service calls instead of direct IP addresses or hostnames.

## 4. Operational Security
- **Health Check Safety**: Ensure `/health` endpoints do not leak sensitive infrastructure data (e.g., full connection strings).
- **Endpoint Protection**: Verify that any public-facing API endpoints have basic validation or rate limiting logic.

---

// turbo
// Run this workflow regularly or before any major deployment to ensure a secure posture.
