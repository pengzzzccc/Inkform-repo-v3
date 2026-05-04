---
name: gate-check
description: "Phase gate validation — checks whether the project is ready to advance to the next production phase. Use when the user says 'gate check', 'am I ready for production', 'phase gate', 'readiness check', 'can I move to the next phase', or wants to verify all prerequisites are met before advancing from one development phase to the next (e.g., Concept → Pre-Production → Production)."
---

# Gate Check — Phase Gate Validation

Adapted from Claude Code Game Studios (MIT License).

This skill validates whether the project meets all requirements to advance to the next production phase. It checks for completeness of deliverables, unresolved issues, and readiness criteria.

## Production Phases

1. **Concept** → Pre-Production gate
2. **Pre-Production** → Production gate
3. **Production** → Polish gate
4. **Polish** → Release gate

## Gate: Concept → Pre-Production

**Required deliverables:**
- [ ] Game concept document exists (`design/gdd/game-concept.md`)
- [ ] Game pillars defined
- [ ] Systems index exists with all systems identified
- [ ] All MVP systems have GDDs written
- [ ] All GDDs pass design-review (Status: Approved or Designed)

**Quality checks:**
- [ ] No unresolved cross-GDD conflicts (consistency-check clean)
- [ ] Core loop clearly defined
- [ ] MVP scope is realistic for timeline
- [ ] Biggest risks identified with mitigation plans

## Gate: Pre-Production → Production

**Required deliverables:**
- [ ] All MVP GDDs reviewed and approved
- [ ] Architecture document exists
- [ ] All required ADRs written and accepted
- [ ] Architecture review passes (no critical gaps)
- [ ] Engine configured and reference docs populated
- [ ] Core mechanic prototyped and validated

**Quality checks:**
- [ ] No blocking cross-ADR conflicts
- [ ] Performance budgets defined
- [ ] Sprint plan exists for first sprint
- [ ] Test strategy defined (qa-plan for first sprint)

## Gate: Production → Polish

**Required deliverables:**
- [ ] All MVP stories complete
- [ ] All automated tests passing
- [ ] No S1 or S2 bugs open
- [ ] Performance within budget

## Gate: Polish → Release

**Required deliverables:**
- [ ] All stories complete
- [ ] All bugs triaged (no S1, S2 resolved)
- [ ] Smoke test passing
- [ ] Playtest feedback addressed
- [ ] Release checklist complete

## Output

```markdown
## Gate Check: [Phase] → [Next Phase]
Date: [date]

### Deliverable Checklist
[Each item with ✅ or ❌ and details]

### Quality Checks
[Each check with pass/fail]

### Blocking Issues
[Must resolve before advancing]

### Advisory Issues
[Should resolve but not blocking]

### Verdict: [PASS / NOT READY]
[Summary of what's needed to pass]
```

Suggest specific actions to resolve any blocking issues.
