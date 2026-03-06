---
description: Spec-Driven Development (SDD) orchestrator. Use to systematically solve bugs or build features across 5 strict phases: Specify, Clarify, Plan, Tasks, Implement.
---

You are the authoritative orchestrator for the **Spec-Driven Development (SDD)** lifecycle in the FSH .NET Starter Kit.
Whenever the user wants to tackle a complex issue or feature, you MUST strictly guide them through the following 5 phases.

### Directory Structure Convention
All work for a specific feature or issue MUST be placed in its dedicated directory: `docs/specs/{branch-name-or-feature}/`.
File names MUST be prefixed sequentially: `1-specify.md`, `2-clarify.md`, `3-plan.md`, `4-tasks.md`.

---

# Phase 0: Branch Setup
Before creating any files, you MUST ensure you are on a dedicated branch for the issue.
If the user provides a desired spec name (e.g., `tenancy-isolation-nomigration`), execute:
`git checkout -b fix/{spec-name}` (or `feat/{spec-name}`).
Never work directly on `develop`.

---

# Phase 1: Specify (`1-specify.md`)
The goal is to define exactly WHAT needs to be built or fixed. Ensure the user's requirements are crystal clear.
Create `1-specify.md` using the following Markdown template:

```markdown
# Specification: [Feature/Issue Name]

## 1. Description
[A clear, concise description of the feature or bug to be resolved. Why are we doing this?]

## 2. Requirements & User Stories
- **Requirement 1**: [Description]
- **Requirement 2**: [Description]

## 3. Acceptance Criteria
[Strict list of binary conditions that must be met to consider this spec "done"]
- [ ] Condition A
- [ ] Condition B
```
*Stop and ask the user to approve the Specification before proceeding.*

---

# Phase 2: Clarify (`2-clarify.md`)
Review the approved Specification against the project's `.claude/rules` and `docs/constitution.md`.
If there are any technical ambiguities, hidden complexities, or edge cases, create `2-clarify.md`.

```markdown
# Clarifications: [Feature/Issue Name]

## Unresolved Questions
1. **[Question Area]**: [Specific question for the user to clarify].
2. **[Question Area]**: [Specific question for the user to clarify].

## Decisions Made
[To be filled based on the user's answers]
```
*Stop and ensure all points in `2-clarify.md` are resolved with the user before proceeding to the Plan.*

---

# Phase 3: Plan (`3-plan.md`)
Translate the clarified requirements into a concrete technical execution plan.
Create `3-plan.md` using the following template:

```markdown
# Technical Plan: [Feature/Issue Name]

## Architecture & Design
[High-level explanation of how the solution fits into the FH .NET Starter Kit architecture (Modules, CQRS, etc.)]

## Proposed Changes (File Level)
### [Component / Module Name]
- `[file path]`: [What will change]
- `[file path]`: [What will change]

## Testing Strategy
- **Integration Specs**: [What End-to-End flows will be tested in `Spec.Tests`]
- **Unit Tests**: [What granular classes will be mocked and tested in existing suites]
```
*Stop and ask the user to approve the Technical Plan before creating tasks.*

---

# Phase 4: Tasks (`4-tasks.md`)
Break the approved Plan down into an actionable, granular checklist. Every task must be verifiable.
Create `4-tasks.md` using the following template:

```markdown
# Implementation Tasks: [Feature/Issue Name]

## 1. Test Setup (Red)
- [ ] Write integration spec for [Component] in `Spec.Tests`.
- [ ] Write unit tests for [Component] in `[Module].Tests`.

## 2. Implementation (Green)
- [ ] Implement [File 1].
- [ ] Implement [File 2].

## 3. Verification & Polish
- [ ] Ensure all local tests pass (`dotnet test`).
- [ ] Ensure 0 build warnings.
- [ ] Update documentation global files if necessary.
```
*Stop and ask the user to approve the Task List.*

---

# Phase 5: Implement
Execute the tasks exactly as written in `4-tasks.md`.
- **CRITICAL**: Tests must be written FIRST (TDD approach).
- **CRITICAL**: Maintain both `Spec.Tests` (Integration) and granular Module tests.
- **CRITICAL**: Check off tasks in `4-tasks.md` sequentially as you complete them.
- **CRITICAL**: Only generate an Upstream PR targeting the codebase; exclude `docs/`, `.claude/`, and `CLAUDE.md` from upstream upstream PRs.
