---
name: brainstorm
description: "Guided game concept ideation — use when the user wants to brainstorm a new game idea, explore game concepts, create a game concept document, or says things like 'I want to make a game', 'game idea', 'brainstorm', 'new game concept'. Takes the user from zero idea to a structured game concept document using professional studio ideation techniques, player psychology frameworks (MDA, Self-Determination Theory, Bartle taxonomy), and structured creative exploration."
---

# Game Concept Brainstorm

Adapted from Claude Code Game Studios (MIT License).

When this skill is activated, guide the user through a collaborative game concept brainstorming session. This is NOT a "generate everything silently" skill — it is a facilitated creative exploration where you act as a creative director helping the user discover and refine their game vision.

## Phase 1: Creative Discovery

Start by understanding the person, not the game. Ask conversationally:

**Emotional anchors:**
- What's a moment in a game that genuinely moved, thrilled, or absorbed you? What specifically created that feeling?
- Is there a fantasy or power trip you've always wanted in a game but never found?

**Taste profile:**
- What 3 games have you spent the most time with? What kept you coming back?
- Are there genres you love? Genres you avoid? Why?
- Do you prefer games that challenge you, relax you, tell you stories, or let you express yourself?

**Practical constraints:**
- What kind of experience do you most want players to have? (Challenge & Mastery / Story & Discovery / Expression & Creativity / Relaxation & Flow)
- What's your realistic development timeline? (Weeks / Months / 1-2 years / Multi-year)
- Where are you in your dev journey? (First game / Shipped before / Professional)

**Synthesize** the answers into a **Creative Brief** — a 3-5 sentence summary of the person's emotional goals, taste profile, and constraints. Read the brief back and confirm it captures their intent.

## Phase 2: Concept Generation

Using the creative brief, generate **3 distinct concepts** using these ideation techniques:

**Technique 1: Verb-First Design** — Start with the core player verb (build, fight, explore, solve, survive, create, manage, discover) and build outward.

**Technique 2: Mashup Method** — Combine two unexpected elements: [Genre A] + [Theme B]. The tension creates the unique hook.

**Technique 3: Experience-First Design (MDA Backward)** — Start from the desired player emotion (sensation, fantasy, narrative, challenge, fellowship, discovery, expression, submission) and work backward to mechanics.

For each concept, present:
- **Working Title**
- **Elevator Pitch** (1-2 sentences — must pass the "10-second test")
- **Core Verb** (the single most common player action)
- **Core Fantasy** (the emotional promise)
- **Unique Hook** (passes the "and also" test: "Like X, AND ALSO Y")
- **Primary MDA Aesthetic** (which emotion dominates?)
- **Estimated Scope** (small / medium / large)
- **Why It Could Work** (1 sentence on market/audience fit)
- **Biggest Risk** (1 sentence on the hardest unanswered question)

Ask the user which concept resonates. Let them pick one, combine elements, or ask for fresh directions.

## Phase 3: Core Loop Design

For the chosen concept, build the core loop collaboratively:

**30-Second Loop** (moment-to-moment): What does the player DO every few seconds? Is this action intrinsically satisfying?

**5-Minute Loop** (short-term goals): What structures moment-to-moment play into cycles? Where does "one more turn" psychology kick in?

**Session Loop** (30-120 minutes): What does a complete session look like? Where are natural stopping points?

**Progression Loop** (days/weeks): How does the player grow? What's the long-term goal?

**Player Motivation Analysis** (Self-Determination Theory):
- **Autonomy**: How much meaningful choice does the player have?
- **Competence**: How does the player feel their skill growing?
- **Relatedness**: How does the player feel connected?

## Phase 4: Pillars and Boundaries

Define **3-5 game pillars**:
- Each pillar has a **name** and **one-sentence definition**
- Each pillar has a **design test**: "If we're debating between X and Y, this pillar says we choose __"
- Pillars should create tension with each other

Define **3+ anti-pillars** (what this game is NOT):
- "We will NOT do [thing] because it would compromise [pillar]"

Confirm pillars with the user before proceeding.

## Phase 5: Player Type Validation

Using the Bartle taxonomy and Quantic Foundry motivation model:
- **Primary player type**: Who will LOVE this game?
- **Secondary appeal**: Who else might enjoy it?
- **Who is this NOT for**: Being clear about who won't like it
- **Market validation**: Are there successful games serving a similar player type?

## Phase 6: Scope and Feasibility

Ground the concept in reality:
- **Target platform**: PC / Mobile / Console / Web / Multiple
- **Engine preference**: Ask if they have one, or suggest based on concept
- **Art pipeline**: What's the art style and how labor-intensive is it?
- **Content scope**: Estimate level/area count, item count, gameplay hours
- **MVP definition**: What's the absolute minimum build that tests "is the core loop fun?"
- **Biggest risks**: Technical, design, market
- **Scope tiers**: Full vision vs. what ships if time runs out

## Phase 7: Generate Game Concept Document

After all phases, generate a game concept document and ask the user where to save it (default: `design/gdd/game-concept.md`). The document should include:

```markdown
# Game Concept: [Working Title]

*Created: [Date]*
*Status: Draft*

## Elevator Pitch
> [1-2 sentences]

## Core Identity
| Aspect | Detail |
| ---- | ---- |
| **Genre** | [Primary + subgenre] |
| **Platform** | [Target] |
| **Target Audience** | [Player type] |
| **Player Count** | [Single/Multi] |
| **Session Length** | [Typical duration] |
| **Estimated Scope** | [Small/Medium/Large with timeline] |
| **Comparable Titles** | [2-3 games] |

## Core Fantasy
[Emotional promise]

## Unique Hook
[What makes this different]

## Game Pillars
[3-5 pillars with design tests]

## Anti-Pillars
[What this game is NOT]

## Core Loop
[30-sec, 5-min, session, progression loops]

## Player Motivation Profile
[SDT analysis: Autonomy, Competence, Relatedness]

## MDA Analysis
[Aesthetics → Dynamics → Mechanics breakdown]

## Scope Tiers
[MVP / Vertical Slice / Alpha / Full Vision]

## Risks
[Technical, Design, Market risks]

## Next Steps
1. Run map-systems to decompose into individual systems
2. Run design-system for each system's GDD
3. Run create-architecture for technical blueprint
```

## Suggest Next Steps

After saving, suggest:
1. Decompose the concept into individual systems (map-systems skill)
2. Author per-system GDDs (design-system skill)
3. Plan the technical architecture
4. Prototype the riskiest system
