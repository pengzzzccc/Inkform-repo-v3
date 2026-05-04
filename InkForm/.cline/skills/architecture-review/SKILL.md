---
name: architecture-review
description: "Validates completeness and consistency of project architecture against all GDDs. Use when the user says 'review architecture', 'architecture review', 'check ADR coverage', 'validate architecture', or wants to verify that all game design requirements have corresponding architectural decisions. Builds a traceability matrix, identifies coverage gaps, detects cross-ADR conflicts, and produces a PASS/CONCERNS/FAIL verdict."
---

# Architecture Review

Adapted from Claude Code Game Studios (MIT License).

This skill validates that the complete body of architectural decisions covers all game design requirements, is internally consistent, and correctly targets the project's engine. It is the quality gate between Technical Setup and Pre-Production.

## Phase 1: Load Everything

Read all inputs:

**Design Documents:**
- All GDDs in `design/gdd/`
- `design/gdd/systems-index.md`

**Architecture Documents:**
- All ADRs in `docs/architecture/`
- `docs/architecture/architecture.md` if it exists

**Project Standards:**
- Any technical preferences or coding standards docs

Report: "Loaded [N] GDDs, [M] ADRs."

## Phase 2: Extract Technical Requirements

For each GDD, extract all **technical requirements** — things the architecture must provide:

| Category | Example |
|----------|---------|
| **Data structures** | "Each entity has health, max health, status effects" |
| **Performance constraints** | "Collision detection must run at 60fps with 200 entities" |
| **Cross-system communication** | "Damage system notifies UI and audio simultaneously" |
| **State persistence** | "Player progress persists between sessions" |
| **Threading/timing** | "AI decisions happen off the main thread" |
| **Platform requirements** | "Supports keyboard, gamepad, touch" |

Produce structured requirement lists with IDs: `TR-[system]-NNN`

## Phase 3: Build Traceability Matrix

For each technical requirement, search the ADRs:

| Status | Meaning |
|--------|---------|
| ✅ **Covered** | An ADR explicitly addresses this |
| ⚠️ **Partial** | An ADR partially covers this |
| ❌ **Gap** | No ADR addresses this |

Build the full matrix:

```
| Requirement ID | GDD | System | Requirement | ADR Coverage | Status |
|---------------|-----|--------|-------------|--------------|--------|
| TR-combat-001 | combat.md | Combat | Hitbox detection < 1 frame | ADR-0003 | ✅ |
| TR-combat-002 | combat.md | Combat | Combo window timing | — | ❌ GAP |
```

## Phase 4: Cross-ADR Conflict Detection

Compare every ADR pair for contradictions:

- **Data ownership conflict**: Two ADRs claim the same data
- **Integration contract conflict**: Incompatible interfaces assumed
- **Performance budget conflict**: Combined budgets exceed frame budget
- **Dependency cycle**: Circular initialization requirements
- **Pattern conflict**: Contradictory communication approaches
- **State management conflict**: Duplicate authority over game state

For each conflict, describe both sides, impact, and resolution options.

**ADR Dependency Ordering:**
- Topological sort of ADR dependencies
- Flag unresolved dependencies (depends on Proposed ADR)
- Detect cycles
- Output recommended implementation order

## Phase 5: Architecture Document Coverage

If `docs/architecture/architecture.md` exists:
- Does every system appear in the architecture layers?
- Does the data flow section cover all cross-system communication?
- Are there orphaned architecture (no corresponding GDD)?

## Phase 6: Output Review Report

```markdown
## Architecture Review Report
Date: [date]
GDDs Reviewed: [N]
ADRs Reviewed: [M]

### Traceability Summary
Total requirements: [N]
✅ Covered: [X]
⚠️ Partial: [Y]
❌ Gaps: [Z]

### Coverage Gaps (no ADR exists)
[Each gap with suggested ADR title and domain]

### Cross-ADR Conflicts
[All conflicts from Phase 4]

### ADR Dependency Order
[Topologically sorted implementation order]

### Architecture Document Coverage
[Missing systems and orphaned architecture]

### Verdict: [PASS / CONCERNS / FAIL]

PASS: All covered, no conflicts
CONCERNS: Some gaps but no blockers
FAIL: Critical gaps or blocking conflicts

### Required ADRs
[Prioritized list, foundation first]
```

Ask before writing the report to `docs/architecture/architecture-review-[date].md`.

## Next Steps

- Write missing ADRs starting with most foundational
- Run gate-check when all blocking issues are resolved
- Re-run this review after each new ADR to verify improvement
