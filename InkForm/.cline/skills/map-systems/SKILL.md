---
name: map-systems
description: "Decompose a game concept into individual systems, map dependencies, prioritize design order, and create a systems index. Use when the user says 'break down my game into systems', 'map systems', 'systems decomposition', 'what systems does my game need', or after completing a game concept brainstorm and needs to identify all the technical systems required."
---

# Map Systems — Game Systems Decomposition

Adapted from Claude Code Game Studios (MIT License).

This skill takes a game concept document and decomposes it into individual game systems, maps their dependencies, assigns priority tiers, and creates an ordered systems index that guides the GDD authoring sequence.

## Phase 1: Read Concept (Required Context)

Read the game concept document (default: `design/gdd/game-concept.md`). If it doesn't exist, tell the user to run the brainstorm skill first.

Also read if they exist:
- `design/gdd/game-pillars.md`
- `design/gdd/systems-index.md` (if exists, resume/update rather than recreate)
- Any existing GDDs in `design/gdd/`

## Phase 2: Systems Enumeration

Extract and identify all systems the game needs. Work collaboratively — present your analysis and ask the user to confirm, add, or remove systems.

For each system identified, determine:
- **Name** (kebab-case for filenames)
- **Category**: Foundation/Infrastructure, Combat, Economy, Progression, Dialogue/Narrative, UI, Audio, AI, Level/World, Camera/Input, Animation, Visual Effects, Character
- **Layer**: Foundation → Core → Feature → Presentation
- **One-line description**
- **Why it exists** (which pillar it serves)

### System Discovery Techniques

1. **Pillar-driven**: For each game pillar, what systems are needed to deliver it?
2. **Loop-driven**: For each core loop tier (30s, 5min, session, progression), what systems power it?
3. **Player-verb-driven**: What does the player DO? Each verb implies systems.
4. **Infrastructure audit**: What invisible systems does every game need? (Save/load, input, scene management, event bus, audio manager, UI framework)

Present the full list and ask the user to review before proceeding.

## Phase 3: Dependency Mapping

For each system, identify:
- **Depends on** (upstream): Systems this one requires to function
- **Depended on by** (downstream): Systems that require this one
- **Hard vs. Soft**: Hard = cannot function without it; Soft = enhanced by it

Present as a dependency table. Flag any circular dependencies.

## Phase 4: Priority Assignment

Assign each system to a priority tier:
- **MVP**: Required for the minimum playable build that tests "is the core loop fun?"
- **Vertical Slice**: Required for a polished slice demonstrating the full experience
- **Alpha**: Required for a content-complete but unpolished build
- **Full Vision**: Nice-to-have for the complete game

Within each tier, determine the **design order** based on dependencies (design upstream systems first).

## Phase 5: Generate Systems Index

Create `design/gdd/systems-index.md`:

```markdown
# Systems Index

*Last Updated: [Date]*

## Progress Tracker
- Total Systems: [N]
- Designed: [0]
- In Review: [0]
- Not Started: [N]

## Design Order

### Foundation Layer
| # | System | Priority | Depends On | Status | Design Doc |
|---|--------|----------|------------|--------|------------|
| 1 | [system] | MVP | None | Not Started | — |

### Core Layer
[same table format]

### Feature Layer
[same table format]

### Presentation Layer
[same table format]

## Dependency Graph (text)
[ASCII or text representation of system dependencies]
```

Ask user for approval before writing.

## Next Steps

After saving, suggest:
1. Start designing the first system in order with the design-system skill
2. Design systems in the order listed — upstream dependencies first
