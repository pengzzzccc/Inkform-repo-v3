---
name: consistency-check
description: "Scan all GDDs against each other to detect cross-document inconsistencies: same entity with different stats, same item with different values, same formula with different variables. Use when the user says 'consistency check', 'check for conflicts', 'cross-reference GDDs', 'are my design docs consistent', or after writing a new GDD and wanting to verify it doesn't contradict existing ones."
---

# Consistency Check

Adapted from Claude Code Game Studios (MIT License).

This skill detects cross-document inconsistencies by comparing all GDDs against each other. It catches what per-section checks may miss and what holistic reviews catch too late.

**When to run:**
- After writing each new GDD
- Before architecture planning
- On demand to check specific entities

## Phase 1: Load All GDDs

Read `design/registry/entities.yaml` if it exists. If not, scan all GDDs directly.

Read all GDD files in `design/gdd/`. Build lookup tables:
- **Entities**: name → stats, source GDD
- **Items**: name → values, source GDD
- **Formulas**: name → variables, output ranges, source GDD
- **Constants**: name → value, unit, source GDD

## Phase 2: Cross-Reference Scan

For each registered name, grep all GDDs for mentions. Compare values:

**Conflict types:**
- **Value mismatch**: Same entity/item has different stats in different GDDs
- **Formula inconsistency**: Same formula defined differently in two places
- **Interface mismatch**: System A says it sends X to System B, but System B expects Y
- **Missing cross-reference**: System A references System B, but B doesn't know about A
- **Stale reference**: GDD references a system that no longer exists or was renamed

## Phase 3: Output Report

```markdown
## Consistency Check Report
Date: [date]
GDDs Scanned: [N]
Entities Checked: [N]

### Conflicts Found

#### CONFLICT: [entity/item/formula name]
- **In [gdd-a].md**: [value A]
- **In [gdd-b].md**: [value B]
- **Resolution needed**: Which GDD is the source of truth?

### Interface Mismatches

#### [System A] → [System B]
- A sends: [data format]
- B expects: [different format]
- Fix: [suggested resolution]

### Missing Cross-References
[One-directional dependencies that should be bidirectional]

### Summary
- Conflicts: [N]
- Interface mismatches: [N]
- Missing cross-refs: [N]
- Clean: [all consistent / issues found]
```

Ask before writing report. Offer to fix conflicts by updating the non-authoritative GDD.

## Next Steps

- Fix any conflicts before designing the next system
- Run design-review on any GDD that was modified
- Run before architecture planning to ensure clean inputs
