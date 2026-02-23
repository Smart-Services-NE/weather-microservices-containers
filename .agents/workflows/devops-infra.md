---
description: Manage containerized environment, Dapr sidecars, and infrastructure lifecycle
---

# DevOps & Infrastructure Workflow

This workflow automates the management of the Podman/Dapr container ecosystem and infrastructure components.

## 1. Container Blueprinting
- **Dockerfile (Containerfile) Generation**: Create multi-stage builds (SDK -> Runtime) for new services following the `CLAUDE.md` template.
- **Compose Integration**: Add service definitions and mandatory Dapr sidecars to `compose.yaml`.
- **Health Checks**: Ensure every service container has a `curl`-based health check targeting `/health`.

## 2. Dapr Orchestration
- **Identity Assignment**: Assign unique `app-id`s for each service.
- **Communication Protocol**: Configure gRPC ports (50001+N) for service invocation and HTTP ports (3500+N) for Dapr sidecar communication.
- **Component Binding**: Ensure `dapr/components` are correctly mounted and referenced in the sidecar configuration.

## 3. Network & Port Management
- **Sequential Port Registry**: Verify and assign the next available ports:
    - Host API Port: `8080 + N`
    - Dapr HTTP Port: `3500 + N`
    - Dapr gRPC Port: `50001 + N`
- **Network Isolation**: Ensure all containers are attached to the `containerapp-network`.

## 4. Infrastructure Maintenance
- **Volume Management**: Ensure data persistence via volumes (e.g., `./[service]-data:/app/data`) for SQLite or other storage.
- **Dependency Chains**: Use `depends_on` with `service_healthy` conditions to ensure infrastructure (Kafka, Dapr Placement) starts before application code.

---

// turbo
// Run this workflow when adding a new microservice or modifying compose.yaml.
