---
name: qa-plan
description: "Generate a QA test plan for a sprint or feature. Use when the user says 'QA plan', 'test plan', 'testing strategy', 'what tests do I need', 'plan QA for this sprint', or needs to know what automated tests, manual test cases, smoke tests, and playtest sign-offs are required. Reads GDDs and story files, classifies stories by test type, and produces a structured plan."
---

# QA Plan

Adapted from Claude Code Game Studios (MIT License).

This skill generates a structured QA plan for a sprint, feature, or individual story. It reads all in-scope files, classifies each by test type, and produces a plan telling developers exactly what to automate, what to verify manually, and what the smoke test scope is.

**Run this before a sprint begins** so the team knows upfront what testing work is required.

## Phase 1: Determine Scope

Ask the user what scope to plan for:
- **Sprint** — read the most recent sprint plan, extract all story references
- **Feature: [system-name]** — find all stories related to that system
- **Story: [path]** — single story file
- **Full epic** — all stories in an epic

## Phase 2: Load Inputs

For each in-scope story, extract:
- Story title and ID
- Story Type (Logic / Integration / Visual-Feel / UI / Config-Data)
- Acceptance criteria
- Implementation files
- GDD and ADR references
- Dependencies on other stories

Load supporting context:
- `design/gdd/systems-index.md`
- For each referenced GDD: read **Acceptance Criteria** and **Formulas** sections only

## Phase 3: Classify Each Story

| Story Type | Indicators |
|---|---|
| **Logic** | Formulas, numerical thresholds, state transitions, AI decisions, calculations |
| **Integration** | Two+ systems interacting, signals across boundaries, save/load, network sync |
| **Visual/Feel** | Animation, VFX, shaders, "feels responsive", timing, visual feedback |
| **UI** | Menus, HUD, buttons, screens, dialogue boxes, tooltips |
| **Config/Data** | Balance tuning values, data files only — no new code logic |

When ambiguous between Logic and Integration, classify as Integration (requires both unit and integration tests).

## Phase 4: Generate Test Plan

```markdown
# QA Plan: [Sprint/Feature Name]
**Date**: [date]
**Scope**: [N stories across N systems]

## Test Summary

| Story | Type | Automated Test Required | Manual Verification |
|-------|------|------------------------|---------------------|
| [title] | Logic | Unit test — tests/unit/[system]/ | None |
| [title] | Integration | Integration test | Smoke check |
| [title] | Visual/Feel | None (not automatable) | Screenshot + sign-off |
| [title] | UI | Interaction walkthrough | Manual step-through |

## Automated Tests Required

### [Story Title] — [Type]
**Test file path**: tests/[unit|integration]/[system]/[story-slug]_test.[ext]
**What to test**:
- [Specific formula or rule from GDD]
- [Each state transition or decision branch]
- [Each side effect that should/shouldn't occur]

**Edge cases to cover**:
- Zero/minimum input values
- Maximum/boundary input values
- Invalid or null input
- [Edge cases from GDD Edge Cases section]

**Estimated test count**: ~[N] tests

## Manual QA Checklist

### [Story Title] — [Type]
**Verification method**: [Screenshot + sign-off / Playtest / Manual step-through]
**Who must sign off**: [designer / lead / qa]
**Evidence to capture**: [screenshot / video / playtest notes]

- [ ] [Specific observable condition]
- [ ] [Another condition]

## Smoke Test Scope

1. Game launches to main menu without crash
2. New game / new session can be started
3. [Primary mechanic changed this sprint]
4. [System with regression risk]
5. Save / load cycle completes
6. Performance within budget (no new frame spikes)

## Playtest Requirements

| Story | Playtest Goal | Min Sessions | Target Player Type |
|-------|--------------|--------------|-------------------|
| [story] | [Question to answer] | [N] | [new/experienced] |

## Definition of Done — This Sprint

A story is DONE when ALL of:
- [ ] All acceptance criteria verified
- [ ] Test file exists for Logic and Integration stories
- [ ] Manual evidence exists for Visual/Feel and UI stories
- [ ] Smoke check passes
- [ ] No regressions introduced
- [ ] Code reviewed
- [ ] Story file updated to Status: Complete
```

## Phase 5: Write Output

Ask before writing to `production/qa/qa-plan-[slug]-[date].md`.

After writing, suggest:
- Share plan with team before sprint implementation
- Create test files at listed paths before marking stories done
- Run smoke-check after all stories are implemented
