---
name: design-review
description: "Reviews a game design document (GDD) for completeness, internal consistency, implementability, and adherence to project design standards. Use when the user says 'review this GDD', 'design review', 'check my design document', 'is this GDD complete', or wants validation before handing a design document to programmers. Checks all 8 required sections, cross-system consistency, formula correctness, and provides a verdict."
---

# Design Review

Adapted from Claude Code Game Studios (MIT License).

This skill reviews a GDD for completeness, consistency, and implementability. Run this before handing a design document to programmers. Best run in a **separate chat session** from the one that authored the GDD for independent critique.

## Phase 1: Load Documents

Read the target GDD in full. Also read:
- `design/gdd/game-concept.md` for project context
- Related GDDs referenced by or implied by the target doc
- `design/gdd/game-pillars.md` if it exists

**Dependency graph validation:** For every system in the Dependencies section, check whether its GDD file exists. Flag missing files as broken references.

**Prior review check:** Check for `design/gdd/reviews/[doc-name]-review-log.md`. If it exists, note the previous verdict and blocking items.

## Phase 2: Completeness Check

Evaluate against the 8 required sections:

- [ ] **Overview** — one-paragraph summary
- [ ] **Player Fantasy** — intended feeling
- [ ] **Detailed Rules** — unambiguous mechanics
- [ ] **Formulas** — all math defined with variables, ranges, examples
- [ ] **Edge Cases** — unusual situations with explicit resolutions
- [ ] **Dependencies** — other systems listed with interfaces
- [ ] **Tuning Knobs** — configurable values with safe ranges
- [ ] **Acceptance Criteria** — testable Given-When-Then conditions

For each section present, evaluate quality:
- Is it specific enough for implementation?
- Does it use placeholders like `[TBD]` or `[To be designed]`?
- Are there vague phrases like "handle appropriately"?

## Phase 3: Consistency and Implementability

**Internal consistency:**
- Do formulas produce values matching described behavior?
- Plug in boundary values (min/max inputs) — do outputs go degenerate?
- Do edge cases contradict main rules?
- Are dependencies bidirectional?

**Implementability:**
- Are rules precise enough for a programmer to implement without guessing?
- Are there "hand-wave" sections with missing details?
- Are performance implications considered?

**Cross-system consistency:**
- Does this conflict with any existing mechanic in other GDDs?
- Does this create unintended interactions?
- Is this consistent with game pillars and tone?

**Formula validation:**
- For every formula, check boundary values:
  - What happens at zero input?
  - What happens at maximum input?
  - Any division by zero possibilities?
  - Any negative output where only positive makes sense?

**Acceptance criteria validation:**
- Are all criteria independently testable?
- Flag subjective phrases: "feels balanced", "works correctly", "performs well"
- Each criterion should be verifiable by a QA tester without reading the GDD

## Phase 4: Output Review

```markdown
## Design Review: [Document Title]

### Completeness: [X/8 sections present]
[List missing or incomplete sections]

### Dependency Graph
[Each dependency and whether its GDD exists]
- ✓ enemy-definition.md — exists
- ✗ loot-system.md — NOT FOUND

### Required Before Implementation
[Numbered list — blocking issues only]

### Recommended Revisions
[Numbered list — important but not blocking]

### Nice-to-Have
[Minor improvements]

### Scope Signal
- **S** — single system, no formulas, <3 dependencies
- **M** — moderate, 1-2 formulas, 3-6 dependencies
- **L** — multi-system, 3+ formulas, may need new ADR
- **XL** — cross-cutting, 5+ dependencies, multiple new ADRs

### Verdict: [APPROVED / NEEDS REVISION / MAJOR REVISION NEEDED]
```

## Phase 5: Next Steps

Based on verdict:

**If APPROVED:**
- Offer to update systems index status to "Approved"
- Offer to append to review log
- Suggest running consistency-check or designing the next system

**If NEEDS REVISION / MAJOR REVISION:**
- Offer to work through blocking items together
- After revisions, recommend re-running design-review in a fresh chat

**Always offer:**
- Update `design/gdd/systems-index.md` status
- Append to `design/gdd/reviews/[doc-name]-review-log.md`
