---
description: Generate a PR against the main branch after feature completion
---

# Create Pull Request Workflow

This workflow automates the final steps of preparing and submitting your code for review.

## 1. Final Validation
- **Linting**: Run `dotnet format` to ensure style consistency.
- **Testing**: Run `dotnet test WeatherApp.sln` one last time.
- **Architectural Check**: Ensure no IDesign layer violations were introduced.

## 2. Git Preparation
- **Branch Check**: Ensure you are on a feature branch (not `main`).
- **Commit**: Stage and commit all changes with a descriptive message.
    - Format: `feat: [feature name] - implementation complete`
- **Sync**: Pull latest from `main` and rebase if necessary.

## 3. Push and PR
- **Push**: Push the current branch to origin.
- **PR Generation**: Use the GitHub CLI (`gh`) to create the PR.
    - Command: `gh pr create --title "feat: [Feature Name]" --body "Implements IDesign layers, TDD, and Observability for [Feature]." --base main`

## 4. Post-PR Verification
- Check CI/CD status in GitHub Actions.
- Review the diff in the PR to ensure no sensitive files (like `.env`) were accidentally included.

---

// turbo
// Run this after obs-resilience.md is completed.
