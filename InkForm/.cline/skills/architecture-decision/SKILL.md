---
name: architecture-decision
description: "Creates an Architecture Decision Record (ADR) documenting a significant technical decision, its context, alternatives considered, and consequences. Use when the user says 'architecture decision', 'ADR', 'technical decision', 'document this architecture choice', or needs to record why a specific technical approach was chosen over alternatives. Every major technical choice should have an ADR."
---

# Architecture Decision Record (ADR)

Adapted from Claude Code Game Studios (MIT License).

This skill guides the user through creating a structured ADR that documents a significant technical decision with full context, alternatives, and consequences.

## Phase 0: Parse Input

If no title is provided, ask: "What technical decision are you documenting? Provide a short title (e.g., `event-system-architecture`, `physics-engine-choice`)."

## Phase 1: Gather Context

1. **Determine the next ADR number** — scan `docs/architecture/` for existing ADRs
2. **Read related context**: existing ADRs, relevant GDDs from `design/gdd/`, any existing architecture docs
3. **Identify the domain**: Physics, Rendering, UI, Audio, Navigation, Animation, Networking, Core, Input, Scripting

**Present assumptions before drafting:**

> Here's what I'm assuming:
> - **Problem**: [one-sentence problem statement from context]
> - **Alternatives to consider**: A) [option], B) [option], C) [option]
> - **GDD systems driving this**: [list from context]
> - **Dependencies**: [upstream ADRs if any]
> - **Status**: Proposed

Ask the user to confirm or adjust before proceeding.

## Phase 2: Collaborative Design

Guide the decision through structured discussion:
- What problem are we solving? Why now?
- What are the constraints (technical, timeline, resources)?
- Present 2-3 concrete approaches with pros/cons
- Help the user evaluate trade-offs
- Arrive at a clear decision

## Phase 3: Generate the ADR

```markdown
# ADR-[NNNN]: [Title]

## Status
Proposed

## Date
[Date]

## Context

### Problem Statement
[What problem are we solving? Why now?]

### Constraints
- [Technical constraints]
- [Timeline constraints]
- [Resource constraints]

### Requirements
- [Must support X]
- [Must perform within Y budget]
- [Must integrate with Z]

## Decision

[Specific technical decision, described in enough detail for implementation]

### Architecture Diagram
[ASCII diagram or description]

### Key Interfaces
[API contracts or interface definitions]

## Alternatives Considered

### Alternative 1: [Name]
- **Description**: [How it would work]
- **Pros**: [Advantages]
- **Cons**: [Disadvantages]
- **Rejection Reason**: [Why not chosen]

### Alternative 2: [Name]
- **Description**: [How it would work]
- **Pros**: [Advantages]
- **Cons**: [Disadvantages]
- **Rejection Reason**: [Why not chosen]

## Consequences

### Positive
- [Good outcomes]

### Negative
- [Trade-offs accepted]

### Risks
- [What could go wrong + mitigation]

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| [system].md | [specific requirement] | [how] |

## Performance Implications
- **CPU**: [Expected impact]
- **Memory**: [Expected impact]
- **Load Time**: [Expected impact]

## Migration Plan
[If changing existing code, how to transition]

## Validation Criteria
[How to verify this decision was correct]

## Related Decisions
- [Links to related ADRs and design docs]
```

## Phase 4: Write and Next Steps

Ask where to save (default: `docs/architecture/adr-[NNNN]-[slug].md`).

After writing, check if any GDD names have been renamed by this ADR's interfaces and flag them.

Suggest next steps:
- Write additional ADRs for remaining technical decisions
- Run architecture-review to validate coverage
- Never run architecture-review in the same session as writing ADRs — use a fresh chat
