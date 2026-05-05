# Narrative Documentation — Index

## Overview

This directory contains the complete narrative design for InkForm: world building, character profiles, full story outline, and the foundational in-universe document that drives the plot — the Willard Protocol.

All documents are written in English and serve as the authoritative reference for narrative implementation.

---

## Reading Order

| # | Document | Description | Status |
|---|----------|-------------|--------|
| 1 | **World_Overview.md** | World history, JARL's three institutions, K-01/K-02 definitions | ✅ Complete |
| 2 | **Characters.md** | Profiles for Mary, Arthur, Ruth, InkForm, K-01, K-02 Shadow | ✅ Complete |
| 3 | **Story_Outline.md** | Chapter 1–3 full plot, branch logic, endings A & B | ✅ Complete (Chapter 3: partial) |
| 4 | **Willard_Protocol.md** | Original Protocol draft + Ruth's Implementation Notes addendum | ✅ Complete |

---

## Document Dependencies

```
World_Overview.md           ← Foundation: what the world is
    ↓
Characters.md               ← Who lives in it
    ↓
Story_Outline.md            ← What happens
    ↓
Willard_Protocol.md         ← The in-universe document that drives the conflict
```

World_Overview and Characters should be read first to understand the setting and motivations. Willard_Protocol is best read after Story_Outline, as it reveals the document that the characters argue about throughout the story.

---

## How to Use

- **Writers**: Start with World_Overview for tone and scope, then Characters for voice, then Story_Outline for scene beats.
- **Designers**: Story_Outline contains all gameplay-relevant triggers (suspicion system, story missions, branch conditions).
- **Programmers**: See `[IMPLEMENTATION NOTE]` tags in Story_Outline for systems that need code support (NPC dialogue, suspicion meter, destructible objects, inventory/collection).

---

## Conventions

- `[TBD]` — To be determined; placeholder for future discussion
- `[IMPLEMENTATION NOTE]` — Directly relevant to code/system design
- `[BRANCH: ...]` — Indicates divergent story path
- *Italic text* — In-universe documents, audio logs, or diegetic text