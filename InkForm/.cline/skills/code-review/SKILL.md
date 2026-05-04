---
name: code-review
description: "Performs an architectural and quality code review on specified files. Use when the user says 'code review', 'review my code', 'review this file', 'check code quality', or wants feedback on coding standards, SOLID principles, architecture patterns, testability, and game-specific performance concerns. Checks for ADR compliance, dependency direction, frame-rate independence, and more."
---

# Code Review

Adapted from Claude Code Game Studios (MIT License).

This skill performs a comprehensive code review checking architectural patterns, coding standards, SOLID principles, testability, and game-specific concerns.

## Phase 1: Load Target Files

Read the target file(s) in full. Also read:
- Project coding standards or style guides if available
- Any referenced ADRs (look for `ADR-NNN` patterns in comments)

## Phase 2: ADR Compliance Check

Search for ADR references in the code (comments, headers, commit messages). For each referenced ADR, read it and check:

- **ARCHITECTURAL VIOLATION** (BLOCKING): Uses a pattern explicitly rejected in the ADR
- **ADR DRIFT** (WARNING): Meaningfully diverges from chosen approach
- **MINOR DEVIATION** (INFO): Small difference, doesn't affect architecture

If no ADR references found, note it and continue.

## Phase 3: Standards Compliance

- [ ] Public methods and classes have doc comments
- [ ] Cyclomatic complexity under 10 per method
- [ ] No method exceeds 40 lines (excluding data declarations)
- [ ] Dependencies are injected (no static singletons for game state)
- [ ] Configuration values loaded from data files (not hardcoded)
- [ ] Systems expose interfaces (not concrete class dependencies)

## Phase 4: Architecture and SOLID

**Architecture:**
- [ ] Correct dependency direction (engine ← gameplay, not reverse)
- [ ] No circular dependencies between modules
- [ ] Proper layer separation (UI does not own game state)
- [ ] Events/signals used for cross-system communication
- [ ] Consistent with established codebase patterns

**SOLID:**
- [ ] **Single Responsibility**: Each class has one reason to change
- [ ] **Open/Closed**: Extendable without modification
- [ ] **Liskov Substitution**: Subtypes substitutable for base types
- [ ] **Interface Segregation**: No fat interfaces
- [ ] **Dependency Inversion**: Depends on abstractions, not concretions

## Phase 5: Game-Specific Concerns

- [ ] Frame-rate independence (delta time usage)
- [ ] No allocations in hot paths (update loops)
- [ ] Proper null/empty state handling
- [ ] Thread safety where required
- [ ] Resource cleanup (no leaks)
- [ ] No magic numbers — gameplay values should be data-driven

## Phase 6: Output Review

```markdown
## Code Review: [File/System Name]

### ADR Compliance: [NO ADRS / COMPLIANT / DRIFT / VIOLATION]
[Details per ADR checked]

### Standards Compliance: [X/6 passing]
[List failures with line references]

### Architecture: [CLEAN / MINOR ISSUES / VIOLATIONS FOUND]
[Specific architectural concerns]

### SOLID: [COMPLIANT / ISSUES FOUND]
[Specific violations]

### Game-Specific Concerns
[Performance, frame-rate, resource issues]

### Positive Observations
[What is done well — always include this]

### Required Changes
[Must-fix items before approval]

### Suggestions
[Nice-to-have improvements]

### Verdict: [APPROVED / APPROVED WITH SUGGESTIONS / CHANGES REQUIRED]
```

This skill is **read-only** — no files are written.

## Next Steps

- If APPROVED: proceed to close the story/task
- If CHANGES REQUIRED: fix issues and re-run code-review
- If ARCHITECTURAL VIOLATION found: consider creating an ADR to document the correct approach
