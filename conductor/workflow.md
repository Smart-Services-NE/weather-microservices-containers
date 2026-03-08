# Project Workflow

## Guiding Principles

1. **The Plan is the Source of Truth:** All work must be tracked in `plan.md`
2. **The Tech Stack is Deliberate:** Changes to the tech stack must be documented in `tech-stack.md` *before* implementation
3. **iDesign Methodology:** Strictly adhere to the iDesign architecture principles as defined in `CLAUDE.md` and verified by `.agents/workflows/arch-compliance.md`.
4. **Test-Driven Development:** Write unit tests before implementing functionality
5. **High Code Coverage:** Aim for 85% code coverage for all modules
6. **User Experience First:** Every decision should prioritize user experience
7. **Non-Interactive & CI-Aware:** Prefer non-interactive commands. Use `CI=true` for watch-mode tools (tests, linters) to ensure single execution.

## Task Workflow

All tasks follow a strict lifecycle:

### Standard Task Workflow

1. **Select Task:** Choose the next available task from `plan.md` in sequential order

2. **Mark In Progress:** Before beginning work, edit `plan.md` and change the task from `[ ]` to `[~]`

3. **Write Failing Tests (Red Phase):**
   - Create a new test file for the feature or bug fix.
   - Write one or more unit tests that clearly define the expected behavior and acceptance criteria for the task.
   - **CRITICAL:** Run the tests and confirm that they fail as expected. This is the "Red" phase of TDD. Do not proceed until you have failing tests.

4. **Implement to Pass Tests (Green Phase):**
   - Write the minimum amount of application code necessary to make the failing tests pass.
   - Run the test suite again and confirm that all tests now pass. This is the "Green" phase.

5. **Refactor (Optional but Recommended):**
   - With the safety of passing tests, refactor the implementation code and the test code to improve clarity, remove duplication, and enhance performance without changing the external behavior.
   - Rerun tests to ensure they still pass after refactoring.

6. **Verify Coverage:** Run coverage reports using the project's chosen tools. For example, in a .NET project, this might look like:
   ```bash
   dotnet test /p:CollectCoverage=true /p:Threshold=85
   ```
   Target: 85% coverage for new code.

7. **Document Deviations:** If implementation differs from tech stack:
   - **STOP** implementation
   - Update `tech-stack.md` with new design
   - Add dated note explaining the change
   - Resume implementation

8. **Draft Task Summary:** Create a detailed summary for the completed task. This should include the task name, a summary of changes, a list of all created/modified files, and the core "why" for the change.

9. **Commit Code Changes:**
   - Stage all code changes related to the task.
   - Perform the commit using the drafted summary as the commit message body.
   - Message Format:
     ```
     <type>(<scope>): <description>

     <Task Summary Content>
     ```

10. **Get and Record Task Commit SHA:**
    - **Step 10.1: Update Plan:** Read `plan.md`, find the line for the completed task, update its status from `[~]` to `[x]`, and append the first 7 characters of the *just-completed commit's* commit hash.
    - **Step 10.2: Write Plan:** Write the updated content back to `plan.md`.

11. **Commit Plan Update:**
    - **Action:** Stage the modified `plan.md` file.
    - **Action:** Commit this change with a descriptive message (e.g., `conductor(plan): Mark task 'Create user model' as complete`).

### Phase Completion Verification and Checkpointing Protocol

**Trigger:** This protocol is executed immediately after a task is completed that also concludes a phase in `plan.md`.

1.  **Announce Protocol Start:** Inform the user that the phase is complete and the verification and checkpointing protocol has begun.

2.  **Ensure Test Coverage for Phase Changes:**
    -   **Step 2.1: Determine Phase Scope:** To identify the files changed in this phase, you must first find the starting point. Read `plan.md` to find the Git commit SHA of the *previous* phase's checkpoint. If no previous checkpoint exists, the scope is all changes since the first commit.
    -   **Step 2.2: List Changed Files:** Execute `git diff --name-only <previous_checkpoint_sha> HEAD` to get a precise list of all files modified during this phase.
    -   **Step 2.3: Verify and Create Tests:** For each file in the list:
        -   **CRITICAL:** First, check its extension. Exclude non-code files (e.g., `.json`, `.md`, `.yaml`).
        -   For each remaining code file, verify a corresponding test file exists.
        -   If a test file is missing, you **must** create one. Before writing the test, **first, analyze other test files in the repository to determine the correct naming convention and testing style.** The new tests **must** validate the functionality described in this phase's tasks (`plan.md`).

3.  **Execute Automated Tests with Proactive Debugging:**
    -   Before execution, you **must** announce the exact shell command you will use to run the tests.
    -   Execute the announced command.
    -   If tests fail, you **must** inform the user and begin debugging. You may attempt to propose a fix a **maximum of two times**. If the tests still fail after your second proposed fix, you **must stop**, report the persistent failure, and ask the user for guidance.

4.  **Verify iDesign Compliance:**
    -   Run the architectural compliance check as defined in `.agents/workflows/arch-compliance.md`.
    -   Ensure namespaces, file placements, and dependency rules are followed.

5.  **Propose a Detailed, Actionable Manual Verification Plan:**
    -   **CRITICAL:** To generate the plan, first analyze `product.md`, `product-guidelines.md`, and `plan.md` to determine the user-facing goals of the completed phase.
    -   You **must** generate a step-by-step plan that walks the user through the verification process, including any necessary commands and specific, expected outcomes.

6.  **Await Explicit User Feedback:**
    -   After presenting the detailed plan, ask the user for confirmation: "**Does this meet your expectations? Please confirm with yes or provide feedback on what needs to be changed.**"
    -   **PAUSE** and await the user's response. Do not proceed without an explicit yes or confirmation.

7.  **Create Checkpoint Commit:**
    -   Stage all changes.
    -   Draft a detailed verification report including the automated test command, the manual verification steps, and the user's confirmation.
    -   Perform the commit with a clear message (e.g., `conductor(checkpoint): Checkpoint end of Phase X`) and include the verification report in the commit message body.

8.  **Get and Record Phase Checkpoint SHA:**
    -   **Step 8.1: Get Commit Hash:** Obtain the hash of the *just-created checkpoint commit* (`git log -1 --format="%H"`).
    -   **Step 8.2: Update Plan:** Read `plan.md`, find the heading for the completed phase, and append the first 7 characters of the commit hash in the format `[checkpoint: <sha>]`.
    -   **Step 8.3: Write Plan:** Write the updated content back to `plan.md`.

9. **Commit Plan Update:**
    - **Action:** Stage the modified `plan.md` file.
    - **Action:** Commit this change with a descriptive message following the format `conductor(plan): Mark phase '<PHASE NAME>' as complete`.

10.  **Announce Completion:** Inform the user that the phase is complete and the checkpoint has been created.

### Quality Gates

Before marking any task complete, verify:

- [ ] All tests pass
- [ ] Code coverage meets requirements (85%)
- [ ] Code follows iDesign architecture principles and `arch-compliance.md` rules
- [ ] Code follows project's code style guidelines (as defined in `code_styleguides/`)
- [ ] All public functions/methods are documented (e.g., XML documentation for C#)
- [ ] Type safety is enforced
- [ ] No linting or static analysis errors
- [ ] Documentation updated if needed
- [ ] No security vulnerabilities introduced

## Development Commands

### Setup
```bash
# Install dependencies and setup local infrastructure
dotnet restore
podman compose up -d
```

### Daily Development
```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test WeatherService/Tests/Managers.Tests

# Start the application
podman compose up --build
```

### Before Committing
```bash
# Run tests and check coverage
dotnet test /p:CollectCoverage=true /p:Threshold=85
```

## Testing Requirements

### Unit Testing
- Every module must have corresponding tests.
- Use appropriate test setup/teardown mechanisms.
- Mock external dependencies (e.g., using Moq).
- Test both success and failure cases.

### Integration Testing
- Test complete microservice flows
- Verify database transactions using SQLite
- Test Dapr service invocation (can be mocked or use Dapr sidecar)
- Test Kafka message consumption/production

## Commit Guidelines

### Message Format
```
<type>(<scope>): <description>

<Task Summary Content>
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Formatting, missing semicolons, etc.
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `test`: Adding missing tests
- `chore`: Maintenance tasks

## Definition of Done

A task is complete when:

1. All code implemented to specification
2. Unit tests written and passing
3. Code coverage meets project requirements (85%)
4. Documentation complete (if applicable)
5. Code passes iDesign compliance checks
6. Implementation notes added to `plan.md`
7. Changes committed with proper message and summary in body
8. First 7 chars of commit hash recorded in `plan.md`
