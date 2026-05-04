---
name: design-system
description: "Guided, section-by-section GDD (Game Design Document) authoring for a single game system. Use when the user wants to write a GDD, design a game system, create a game design document, or says things like 'design the combat system', 'write a GDD for inventory', 'design-system movement'. Gathers context from existing docs, walks through each required section collaboratively, cross-references dependencies, and writes incrementally to file."
---

# Design System — GDD Authoring

Adapted from Claude Code Game Studios (MIT License).

This skill guides the user through writing a complete Game Design Document for a single game system. It follows a strict section-by-section collaborative approach — never auto-generate the full GDD silently.

## Prerequisites

A system name is **required**. If not provided, check `design/gdd/systems-index.md` for the next undesigned system. If no index exists, ask the user to run the map-systems skill first.

## Phase 1: Gather Context

Before asking the user anything, read all relevant context:

**Required reads:**
- `design/gdd/game-concept.md` — fail if missing ("Run brainstorm first")
- `design/gdd/systems-index.md` — fail if missing ("Run map-systems first")

**Dependency reads:**
- Identify upstream dependencies from the systems index
- Read their GDDs if they exist (extract key interfaces, formulas, edge cases)
- Identify downstream dependents

**Optional reads:**
- `design/gdd/game-pillars.md`
- Existing GDD for this system (resume if partially written)
- Related GDDs

**Present Context Summary:**
> **Designing: [System Name]**
> - Priority: [from index] | Layer: [from index]
> - Depends on: [list, noting which have GDDs vs. undesigned]
> - Depended on by: [list]
> - Existing decisions to respect: [key constraints]
> - Pillar alignment: [which pillar(s) this system serves]

Warn if upstream dependencies are undesigned.

## Phase 2: Create File Skeleton

Create the GDD file with empty section headers at `design/gdd/[system-name].md`:

```markdown
# [System Name]

> **Status**: In Design
> **Author**: [user]
> **Last Updated**: [today's date]
> **Implements Pillar**: [from context]

## Overview
[To be designed]

## Player Fantasy
[To be designed]

## Detailed Design
### Core Rules
[To be designed]
### States and Transitions
[To be designed]
### Interactions with Other Systems
[To be designed]

## Formulas
[To be designed]

## Edge Cases
[To be designed]

## Dependencies
[To be designed]

## Tuning Knobs
[To be designed]

## Acceptance Criteria
[To be designed]

## Open Questions
[To be designed]
```

## Phase 3: Section-by-Section Design

Walk through each section in order. For **each section**, follow this cycle:

```
Context → Questions → Options → Decision → Draft → Approval → Write
```

1. **Context**: State what this section needs and surface constraints from dependency GDDs
2. **Questions**: Ask clarifying questions specific to this section
3. **Options**: Present 2-4 design approaches with pros/cons where applicable
4. **Decision**: User picks an approach
5. **Draft**: Write the section content for review
6. **Approval**: Ask if the user approves or wants changes
7. **Write**: Update the file with approved content

### Section-Specific Guidance

**A. Overview** — One paragraph a stranger could understand. What is this system, how does a player interact with it, why does it exist?

**B. Player Fantasy** — The emotional target. What should the player FEEL? Reference games that nail this feeling. Must align with game pillars.

**C. Detailed Design** — The largest section. Break into:
- **Core Rules**: Numbered rules for sequential processes, bullets for properties. Must be precise enough for a programmer to implement without guessing.
- **States and Transitions**: Map every state and valid transition in a table.
- **Interactions with Other Systems**: For each dependency, specify data flow in/out and interface ownership.

**D. Formulas** — Every mathematical formula with variables defined, ranges specified:
```
[formula_name] = [expression]

| Variable | Type | Range | Description |
|----------|------|-------|-------------|
| [name]   | float| [min–max] | [what it represents] |

Output Range: [min] to [max]
Example: [worked example with real numbers]
```

**E. Edge Cases** — Format each as:
- **If [condition]**: [exact outcome]. [rationale if non-obvious]

**F. Dependencies** — Map every connection: direction (upstream/downstream), hard vs. soft, specific data interface.

**G. Tuning Knobs** — Designer-adjustable values with safe ranges and what breaks at extremes.

**H. Acceptance Criteria** — Given-When-Then format:
- **GIVEN** [initial state], **WHEN** [action], **THEN** [measurable outcome]

Include at least one criterion per core rule and one per formula.

## Phase 4: Post-Design Validation

After all sections are written:

1. **Self-check**: Verify all 8 sections have real content, formulas reference defined variables, edge cases have resolutions, dependencies list interfaces, acceptance criteria are testable.

2. **Update systems index**: Update the system's status to "Designed" in `design/gdd/systems-index.md`.

3. **Suggest next steps**:
   - Run design-review on this GDD (recommend in a new chat for independent review)
   - Run consistency-check to verify cross-GDD values
   - Design the next system in order
   - Run gate-check when all MVP GDDs are done

## Recovery & Resume

If returning to a partially written GDD:
1. Read the file — sections with real content are done; `[To be designed]` sections still need work
2. Resume from the next incomplete section
