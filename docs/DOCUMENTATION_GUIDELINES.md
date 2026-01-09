# Documentation Guidelines

Standard patterns and practices for creating documentation in this project.

## Directory Structure

All documentation lives in the `docs/` folder with this structure:

```
docs/
├── README.md                    # Main navigation hub
├── getting-started/             # Setup and onboarding guides
├── kafka/                       # Kafka and messaging docs
├── services/                    # Service-specific documentation
├── infrastructure/              # Infrastructure components
└── scripts/                     # Automation and tooling
```

## Document Structure

### Required Sections

Every documentation file should include:

1. **Title** (H1) - Clear, descriptive
2. **Overview** - Brief description (1-2 sentences)
3. **Prerequisites** (if applicable)
4. **Main Content** - Organized with H2/H3 headings
5. **Examples** - Code samples and commands
6. **Troubleshooting** - Common issues and solutions
7. **Related Documentation** - Links to other relevant docs

### Example Template

```markdown
# Feature Name

Brief description of what this document covers.

## Prerequisites

- Requirement 1
- Requirement 2

## Quick Start

Fastest way to get started:

```bash
# Example command
docker-compose up -d
```

## Detailed Guide

### Step 1: Configuration

Instructions...

### Step 2: Execution

Instructions...

## Examples

### Example 1: Common Use Case

```bash
# Command
```

**Expected Output**:
```
Output example
```

## Troubleshooting

### Issue: Problem Description

**Error**: `Error message`

**Solution**:
1. Step 1
2. Step 2

## Related Documentation

- [Related Doc 1](../path/to/doc.md)
- [Related Doc 2](../path/to/doc.md)
```

## Writing Style

### Conciseness
- Get to the point quickly
- Use bullet points for lists
- Keep sentences short and clear
- Remove redundant information

### Clarity
- Use code blocks with language identifiers
- Include **both** commands **and** expected output
- Mark critical steps with **bold** or **IMPORTANT**
- Use tables for structured comparisons

### Code Examples

Always include:
```markdown
```bash
# Brief description of what this does
docker-compose up -d

# Expected output (if relevant)
# Creating network "containerapp_network" ... done
# Creating containerapp-kafka-1 ... done
```
```

### Command Output

Show expected output for verification:
```markdown
**Expected**:
```
[Information] Service started successfully
```

**Error Indicator**:
```
[Error] Connection failed
```
```

## Cross-Referencing

### Internal Links

Use relative paths:
```markdown
- [Kafka Setup](../kafka/confluent-cloud-setup.md)
- [Main README](../../README.md)
- [Troubleshooting](./troubleshooting.md)
```

### Link Patterns

- **Getting Started**: Link to setup guides
- **Troubleshooting**: Link from feature docs
- **Related Docs**: Link to complementary guides
- **Main README**: Link to docs/README.md for navigation

### When to Link

Link when:
- Referencing a concept explained elsewhere
- Pointing to setup/configuration
- Directing to troubleshooting
- Connecting related features

## Content Organization

### Consolidation

When multiple docs cover related topics:

1. **Identify Overlap**: Find common themes
2. **Create Sections**: Use H2/H3 to organize
3. **Merge Content**: Combine without duplication
4. **Cross-Reference**: Link to related topics
5. **Remove Old Files**: Delete originals after consolidation

### Example Consolidation

**Before**:
- `KAFKA_SETUP.md`
- `KAFKA_QUICK_START.md`
- `KAFKA_TROUBLESHOOTING.md`

**After**:
- `docs/kafka/confluent-cloud-setup.md` (setup + quick start)
- `docs/kafka/troubleshooting.md` (all troubleshooting)

### File Naming

- Use **kebab-case**: `quick-start.md`, `notification-service.md`
- Be descriptive: `confluent-cloud-setup.md` not `kafka.md`
- Avoid redundancy: `kafka/setup.md` not `kafka/kafka-setup.md`

## Formatting Standards

### Headings

```markdown
# Document Title (H1 - once per file)

## Major Section (H2)

### Subsection (H3)

#### Detail (H4 - use sparingly)
```

### Code Blocks

Always specify language:
```markdown
```bash
docker-compose up -d
```

```json
{
  "key": "value"
}
```

```csharp
public class Example
{
    // Code
}
```
```

### Tables

Use tables for comparisons:
```markdown
| Feature | Option A | Option B |
|---------|----------|----------|
| Speed | Fast | Slow |
| Complexity | Simple | Complex |
```

### Lists

**Ordered** for sequences:
```markdown
1. First step
2. Second step
3. Third step
```

**Unordered** for features/options:
```markdown
- Feature one
- Feature two
- Feature three
```

### Emphasis

- **Bold** for important terms and actions
- *Italic* for emphasis (use sparingly)
- `Code formatting` for commands, files, variables
- > Blockquotes for notes/warnings

### Call-outs

```markdown
**IMPORTANT**: Critical information

**Note**: Additional context

**Warning**: Caution required

✅ **Success Indicator**

❌ **Error Indicator**
```

## Documentation Types

### Getting Started Guides

**Purpose**: Help users start quickly

**Include**:
- Prerequisites
- Quick start (< 5 minutes)
- Common tasks
- Next steps

**Keep**:
- Short (< 500 lines)
- Action-oriented
- Example-heavy

### Reference Documentation

**Purpose**: Comprehensive information

**Include**:
- All configuration options
- Complete API reference
- All commands/options
- Technical details

**Keep**:
- Well-organized
- Searchable (clear headings)
- Complete but not redundant

### Troubleshooting Guides

**Purpose**: Solve common problems

**Include**:
- Error messages (exact text)
- Root causes
- Step-by-step solutions
- Diagnostic commands

**Structure**:
```markdown
### Problem: Brief Description

**Error**: `Exact error message`

**Cause**: Why this happens

**Solution**:
1. Step one
2. Step two

**Verify**:
```bash
# Verification command
```
```

### How-To Guides

**Purpose**: Accomplish specific tasks

**Include**:
- Clear objective
- Prerequisites
- Step-by-step instructions
- Verification steps

**Keep**:
- Task-focused
- Practical examples
- Outcome-oriented

## Maintenance

### When to Update

Update documentation when:
- Adding new features
- Changing configuration
- Fixing bugs that affect setup
- Users report confusion
- Tools/versions change

### Review Checklist

Before committing documentation:
- [ ] All commands tested and work
- [ ] Code examples are correct
- [ ] Links work (no 404s)
- [ ] Markdown renders correctly
- [ ] No spelling/grammar errors
- [ ] Related docs updated
- [ ] Main README links updated (if needed)

### Deprecation

When removing features:
1. Mark as deprecated in docs
2. Add migration guide
3. Keep docs for 1+ version
4. Remove after transition period

## Examples

### Good Documentation

✅ **Clear Title**:
```markdown
# Kafka Troubleshooting Guide
```

✅ **Tested Commands**:
```markdown
```bash
# Check service health
curl http://localhost:8082/health

# Expected output
{"status":"Healthy"}
```
```

✅ **Linked References**:
```markdown
See [Confluent Cloud Setup](./confluent-cloud-setup.md) for credentials.
```

### Poor Documentation

❌ **Vague Title**:
```markdown
# Guide
```

❌ **Untested Commands**:
```markdown
Just run the docker command and it should work
```

❌ **Broken Links**:
```markdown
See [Setup](../setup.md) ← File doesn't exist
```

## Tools

### Markdown Linting

Use markdownlint or similar:
```bash
# Check markdown files
markdownlint docs/**/*.md
```

### Link Checking

Verify all links work:
```bash
# Check for broken links
markdown-link-check docs/**/*.md
```

### Spell Checking

Run spell check before committing:
```bash
# Use VS Code spell checker or
aspell check docs/README.md
```

## Navigation

### Main README (docs/README.md)

The `docs/README.md` serves as the **navigation hub**:

- **Quick Links** - Most common destinations
- **Structure Overview** - What's where
- **Common Tasks** - Frequent actions
- **Troubleshooting** - Quick problem solving

Update when:
- Adding new doc files
- Reorganizing structure
- Adding new categories

### Project README (README.md)

The root `README.md` serves as the **project overview**:

- **Quick Start** - Get running fast
- **Architecture** - High-level overview
- **Link to docs/** - Point to detailed docs
- **Common Tasks** - Most frequent operations

Keep:
- Concise (< 300 lines)
- Quick start focused
- Links to docs for details

## Commit Messages

When updating documentation:

```bash
# Good commit messages
git commit -m "docs: add Kafka troubleshooting guide"
git commit -m "docs: consolidate NotificationService monitoring docs"
git commit -m "docs: fix broken links in getting-started"

# Pattern: docs: <verb> <what>
```

## Questions?

When in doubt:
1. Check existing docs for patterns
2. Follow this guide
3. Prioritize clarity over brevity
4. Test all commands before documenting
5. Get feedback from users

## Summary

**Key Principles**:
- **Concise** - Remove redundancy
- **Clear** - Use examples
- **Connected** - Cross-reference
- **Current** - Keep updated
- **Complete** - Cover edge cases

**File Organization**:
- Group by topic (kafka/, services/, etc.)
- One topic per file (focused scope)
- Consolidate related content
- Use descriptive names

**Writing Style**:
- Action-oriented (do this, run that)
- Example-heavy (show, don't just tell)
- Error-aware (include troubleshooting)
- Reference-rich (link to related docs)
