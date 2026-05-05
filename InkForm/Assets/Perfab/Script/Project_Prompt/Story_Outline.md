# Story Outline — InkForm

## Chapter Flow

```
CHAPTER 1: The Escape
    ├── Tutorial / Training Area (Ruth's audio guidance)
    ├── Escape Sequence
    ├── [BRANCH DECISION POINT]
    │
    ├── Branch 1: Hidden Room → Meet Ruth → Receive mission
    │       └── Factory Disguise → Arrive at Nurserie
    │
    └── Branch 2: Bypass hidden room → Factory floor
            └── Hack K-01 terminal → Discover Ruth/Mary file
            └── Factory Disguise → Arrive at Nurserie
    │
    ▼
CHAPTER 2: The Nurserie
    ├── Arrival: InkForm disguised as K-01 worker
    ├── Suspicion System active
    ├── Story Mission 1: Archives
    ├── Story Mission 2: Observation Deck
    ├── Story Mission 3: Signal
    └── Arrest Event → Mary follows
    │
    ▼
CHAPTER 3: The Control Center
    ├── Arrival at Control Center
    ├── Confrontation: Arthur & Ruth Willard
    ├── The Willard Protocol revealed
    ├── [FINAL CHOICE]
    │
    ├── ENDING A: Destroy the Protocol
    └── ENDING B: Execute the Protocol
```

---

## Chapter 1 — The Escape

**Setting**: Laboratory Factory — experimental wing and factory floor

**Context**: InkForm is a K-02 prototype under development. During a routine training session, it begins displaying emergent self-awareness. Ruth Willard, observing remotely, makes a decision.

---

### Tutorial Sequence (Both Branches)

The game opens with InkForm in a training chamber. The player learns basic movement and form-switching mechanics through a series of guided exercises.

**Audio Guidance**: Throughout the tutorial, the player hears voice instructions. Initially, these come through the chamber's public address system — a distant, reverberant, institutional broadcast. The voice is female, professional, detached.

**Gameplay**:
1. Solid form movement and sprint (S_Soild_sprint)
2. Fluid form movement and wall climbing (S_fluid_climb)
3. Form switching on command
4. Basic obstacle navigation — breakable glass tube, narrow pipe

**Narrative Beat**: As the player approaches the final training objective — a window at the far end of the chamber — the audio changes. The same voice, but now **intimate, close, a whisper directly at the player's ear**:

> *"Break the window. Escape."*

The window is destructible. Breaking it begins the escape sequence.

[IMPLEMENTATION NOTE: Audio spatialization required — the transition from PA system reverb to close-mic whisper is a key narrative moment. Use S_AudioManager with distinct audio sources for each mode.]

---

### Escape Sequence (Both Branches)

InkForm flees through the laboratory complex. Guards are alerted. The player navigates maintenance corridors, ventilation shafts, and testing chambers while avoiding patrols.

**Key Environment Elements**:
- Destructible glass tubes (drop skill shards)
- Locked doors (require fluid form to bypass through gaps)
- Guard patrols (avoid or use solid form to break through)
- Camera drones (trigger alerts)

[TBD: Specific level layout and exact guard patrol patterns — to be designed during level creation.]

---

### Branch Decision Point

At a critical junction in the escape route, the player faces two paths:
- **Path A**: A dimly lit maintenance corridor. Faint light at the end. Leads to a hidden observation room. → **Branch 1**
- **Path B**: A service elevator down to the factory floor. Sounds of machinery below. → **Branch 2**

The choice is environmental — no explicit UI prompt. The player navigates toward whichever path they find/choose.

---

### Branch 1 — The Hidden Room

InkForm enters a small, concealed observation room overlooking the training chamber. The room contains monitoring equipment, notes, and **Ruth Willard**.

**Ruth's Dialogue** (key beats):
1. Ruth is not surprised to see InkForm. She has been waiting.
2. She speaks of InkForm not as a project, but as a child: *"I consider you my child."*
3. She reveals Mary's existence: *"Your sister Mary is isolated in the Nurserie. As your mother, I ask you — please go there. See her."*
4. She provides the escape route: *"Break the window here. Enter the factory. Hide among the workers on the production line. That is your only way to the Nurserie."*

**Ruth's Motivation** (as established): Ruth is torn between "Existence is Justice" (the Protocol's logic) and "Meaning is Truth" (her maternal instinct). She helped InkForm escape because she cannot accept that her two children — InkForm and Mary — should be sacrificed to a plan that erases all connection between beings. This is her subconscious "selfishness" — the very thing the Protocol was designed to eliminate.

[IMPLEMENTATION NOTE: This is a dialogue scene using S_NPCDialogue. Ruth's dialogue is linear — no branching choices required here. The player receives the mission direction.]

**After dialogue**: InkForm smashes the observation window, drops into the factory, and disguises itself among K-01 units on the production line. The chapter ends with InkForm aboard a transport vehicle heading to the Nurserie.

---

### Branch 2 — The Factory Alone

InkForm takes the service elevator to the factory floor without encountering Ruth. The factory is vast — assembly lines, K-01 units performing repetitive tasks, Old World human supervisors in the background.

**Discovery Sequence**: While hiding from pursuing guards, InkForm stumbles upon a **K-01 cluster management terminal** — a data access point for coordinating labourer units. InkForm interfaces with it.

The terminal displays personnel files. InkForm is drawn to one:

```
> Personnel File: Ruth Willard
> Role: Project InkForm Lead
> Status: Active
> Related: Mary Willard (Nurserie, Block C-7)
> Note: Biological daughter. Inter-generational contact prohibited
         per JARL Directive.
> Attached: [Image — Mary_Willard_Age4.jpg]
```

**Narrative Beat**: InkForm has just gained self-awareness and is grappling with "Who am I? Why do I exist?" Seeing the face of its creator's biological child — a being just as isolated as itself — sparks a new impulse. InkForm is not being guided by a mother's request. It is **choosing to seek answers on its own**. It wants to understand what "creation" means, and what Ruth projected onto it when she made it.

**Factory Disguise**: InkForm learns to mimic K-01 labourer behaviour — walking in formation, performing assembly gestures. It blends into a production line and boards the same transport to the Nurserie.

[IMPLEMENTATION NOTE: The terminal interaction is a brief UI moment — the player activates the terminal and the personnel file is displayed as diegetic text on screen. The K-01 mimicry section is a short gameplay sequence where the player must match K-01 movement patterns to avoid alerting supervisors.]

---

### Chapter 1 Narrative Comparison

| | Branch 1 (Met Ruth) | Branch 2 (Alone) |
|---|---|---|
| **Drive** | Mother's request → passive mission | Self-discovered clue → active search |
| **Knows Ruth?** | Face, voice, words | Name, photo, data |
| **Knows Mary?** | "Your sister" — as defined by Ruth | "Creator's daughter" — name on a screen |
| **Emotional tone** | Warm connection seeded | Lonely curiosity |
| **Arrival at Ch2** | Carrying Ruth's hope | Carrying self-made questions |

---

## Chapter 2 — The Nurserie

**Setting**: The Nurserie — a domed greenhouse island surrounded by seawater

**Context**: InkForm arrives disguised as a K-01 labourer. Its mission (either Ruth's request or its own curiosity) is to find and observe Mary.

---

### The Suspicion System

The Nurserie is under surveillance. InkForm's disguise is imperfect, and its unauthorized presence is tracked by a **Suspicion Meter**:

| Trigger | Suspicion Gain |
|---------|---------------|
| Separated from K-01 worker groups for too long | +10 / second |
| Caught on surveillance camera in restricted zone | +30 (one-time per camera) |
| Violating worker protocol (wrong route, wrong action) | +20 per incident |
| Entering archive vault without authorization | +50 |
| Entering observation deck | +40 |
| Accessing communications tower | +60 |

**Thresholds**:
- **0–33**: Normal. Guards follow standard patrols.
- **34–66**: Elevated. Additional patrols dispatched. Guards move faster.
- **67–99**: Critical. Guards actively search. Alarm pre-warning sounds.
- **100/100**: **Arrest Event triggered** — guards storm InkForm's location with EMP weaponry.

**Alternative Trigger**: If the player completes **all 3 story missions**, the arrest is triggered via wide-area EMP regardless of suspicion meter value. The story missions are the "canon" path; high suspicion arrest is a fail-state variant.

[IMPLEMENTATION NOTE: S_SuspicionSystem — a new manager component. Updates meter on trigger events, dispatches S_GameEvent.OnSuspicionChanged(value). At 100 or after mission 3 completion, fires S_GameEvent.OnArrestTriggered(). Guard NPCs (S_NPCEnemy) listen to these events to adjust behaviour.]

---

### Story Mission 1 — Archives

**Objective**: Infiltrate the Nurserie archive vault and access Mary Willard's personal file.

**Gameplay**:
- Navigate to the archive vault while avoiding K-01 patrols.
- Use fluid form to slip through a maintenance gap into the vault.
- Interface with the archive terminal to retrieve Mary's file.

**Revealed Lore**: Mary's file contains:
- Basic biometric and intake data.
- Parent designation: "A. Willard / R. Willard — Contact Prohibited."
- A flagged annotation: *"Possible Protocol Exception — R.W."* (Ruth's unauthorized note).
- Behavioural notes: *"Subject exhibits atypical curiosity. Multiple unauthorized questions logged. Recommend observation increase."*

**Mary's Reaction**: Elsewhere in the Nurserie, Mary's personal terminal shows a fleeting notification: *"Record Accessed — Auth Unknown."* Mary notices. She does not report it.

**Suspicion**: +20 on archive access.

---

### Story Mission 2 — Observation Deck

**Objective**: Access the observation deck overlooking the children's habitation area and observe Mary's daily life.

**Gameplay**:
- Navigate to the observation deck on the upper ring of the Nurserie.
- Avoid surveillance cameras — the observation deck is a restricted zone.
- Use the one-way glass to observe the children's play area.

**Narrative Content**: The player watches Mary interact with other children in the habitation zone. Through diegetic audio (children's voices), the player hears Mary ask:

> *"What is outside?"*

Another child responds: *"There is no outside. The sky goes to the walls."*

Mary: *"But who made the walls?"*

The other child has no answer. They return to playing. Mary stands alone for a moment, looking up at the Firmament.

**Key Beat**: InkForm realizes Mary is different — just as InkForm is different. Both are beings who ask questions they were not designed to ask. In Branch 1, this confirms what Ruth implied. In Branch 2, this is the first moment of kinship InkForm has ever felt.

**Mary's Reaction**: Mary notices a flicker of light behind the one-way glass. She turns to look directly at it — directly at InkForm, though she cannot see through. A friend asks what she's looking at. She says, *"Nothing."*

**Suspicion**: +40 on entering observation deck.

---

### Story Mission 3 — Signal

**Objective**: Breach the Nurserie communications tower and send a message to Mary's terminal.

**Gameplay**:
- Navigate to the communications tower — the most heavily guarded area.
- Use fluid form to climb the tower's exterior.
- Interface with the broadcast relay to target Mary's personal terminal.

**The Message**: InkForm composes a single short transmission.

> *"Not alone."*

This is the first time InkForm has communicated directly with another being of its own volition. The message is as much for itself as for Mary — an assertion that two isolated consciousnesses have found each other.

**Mary's Reaction**: Mary receives the message on her terminal. She stares at it silently. Before she can respond, alarms blare across the Nurserie.

**Arrest Trigger**: Upon transmission completion, the wide-area EMP is deployed. The arrest is immediate and unavoidable. Guards storm the communications tower. InkForm is disabled by EMP and captured.

Mary witnesses the arrest from a corridor — guards dragging an InkForm entity she now recognizes as the source of the anomalies, the observation, and the message. She makes a decision.

**Suspicion**: +60 on tower access. After mission completion, +50 → instant 100/100.

[IMPLEMENTATION NOTE: The message text "Not alone" should appear as diegetic text on Mary's terminal screen — a simple, centered message on an otherwise blank display. This is a visual moment, not a dialogue line.]

---

### Arrest to Chapter End

**Cutscene**: K-01 guards carry the disabled InkForm to a transport vehicle. Mary watches from hiding. As the vehicle departs for the Control Center, Mary slips through a maintenance hatch and follows — either aboard the same transport or through a parallel service route.

**Chapter End State**:
- InkForm: Captured, en route to Control Center.
- Mary: Stealthily following, undetected by guards (EMP blast masked her movement).
- The connection between them is established but fragile.

---

## Chapter 3 — The Control Center

**Setting**: The Control Center — JARL's nerve centre, a monolithic structure housing the Control Hub

**Context**: InkForm is brought to the Control Center for final processing. Mary secretly follows.

[TBD: Detailed scene-to-scene breakdown for Chapter 3 — the following is the structural framework.]

---

### Arrival and Confrontation

InkForm is brought to the core chamber of the Control Center — the room containing the **Control Hub** (the physical interface for K-02 AGI integration). Arthur and Ruth Willard are present.

Mary emerges from hiding at a key moment — her presence shocks Arthur and Ruth, who have not seen their daughter since her birth.

**Scene Dynamics**:
- **Arthur**: Attempts to maintain control. Explains the Willard Protocol.
- **Ruth**: Silent for much of the scene. Her eyes are on Mary and InkForm.
- **Mary**: Confused, frightened, but present. Her first moments with her parents are under the shadow of an extinction plan.

---

### The Willard Protocol Revealed

Arthur presents the Willard Protocol in full — either as a document, a holographic projection, or spoken dialogue. The player (and Mary) learn:

- The Protocol's four phases: Quarantine, Automation, Cleansing, Rebirth.
- That InkForm was designed to execute Phase II–IV — to become the AGI manager.
- That the "Final Exit" — the extinction of the Old World generation — is imminent.

**Arthur's Argument** (paraphrased): The Willard Protocol is not cruelty — it is the only mathematically sound solution to a species doomed by its own nature. Every war, every atrocity, every extinction event in human history traces to the same root: **"mine" and "yours."** Family teaches "mine." Labour justifies "mine." The only way to break the cycle is to ensure no human ever learns those words. This requires two things: a blank-slate generation, and an impartial executor — InkForm.

---

### The Final Choice

Arthur presents the choice to InkForm:

1. **Accept the Protocol**: Merge with the Control Hub. Become K-02 Shadow. Execute Phase III — the Final Exit. Manage the blank-slate world forever.

2. **Refuse the Protocol**: Destroy the Control Hub. Prevent the Final Exit. Let the Old World generation die naturally. Let Mary and the other children inherit the ruins — with all the risk of repeated cycles.

**Ruth's Silence**: Ruth does not argue. She has already made her choice — she helped InkForm escape. Now she can only watch which path InkForm takes. Her expression communicates: *"I understand either choice."*

**Mary's Presence**: Mary does not fully understand the stakes, but she understands that InkForm is being asked to decide something irreversible. Her presence — a child who is both the Protocol's beneficiary and its victim — is the emotional weight on the scale toward Ending A.

[IMPLEMENTATION NOTE: This is the most complex dialogue scene. S_NPCDialogue with Arthur as the speaker. Branching dialogue based on: Branch 1 vs Branch 2 history, collected documents from Chapter 1/2 destructible objects, and the final choice itself. S_GameEvent.OnStoryTrigger("FinalChoice_A") or ("FinalChoice_B") fires on selection.]

---

## Ending A — Destroy the Protocol

**Player Action**: Control InkForm in **solid form** to physically destroy the Control Hub.

**Gameplay**: A final action sequence: the player navigates the chamber, breaking Control Hub components while the structure destabilizes. Arthur attempts to stop InkForm; Ruth does not. Mary watches.

**Narrative Resolution**:
- The Control Hub shatters. The building begins to collapse.
- Arthur and Ruth rush to protect Mary — a moment of pure parental instinct overcoming ideology.
- InkForm, in solid form, uses its body to shield Mary's family from falling debris.
- The four beings — Arthur, Ruth, Mary, InkForm — are together in a pocket of safety as the Control Center crumbles.

**Closing Narration**:

> *"Freedom is not a state, but the right to make mistakes — and to be selfish. Though the cycle may begin again, at least in this moment, we are bound together by love."*

**End State**: The Willard Protocol is terminated. The Old World generation will die naturally. Mary and the other children will inherit a ruined world with no guarantee of doing better. But they will have **each other** — and the freedom to fail, and to choose.

Screen fades to black. Credits roll. Player returns to start screen.

---

## Ending B — Execute the Protocol

**Player Action**: Control InkForm in **fluid form** to merge with the Control Hub.

**Gameplay**: A final traversal sequence: the player navigates through conduits, data streams, and physical interfaces to reach the Control Hub's core, using fluid form to flow through the machinery.

**Narrative Resolution**:
- InkForm merges with the system. Becomes K-02 Shadow — the AGI manager.
- Phase III activates. All Old World humans — including Arthur and Ruth — are instantaneously terminated.
- Mary witnesses this. Her parents — whom she met only minutes ago — are gone. She collapses to her knees, eyes empty.
- Two K-01 units approach Mary. They take her away. Her memories are erased.
- Mary is returned to the Nurserie. She resumes life as before — a child under an artificial sky, with no memory of parents, of InkForm, of the message *"Not alone."*

**Final Image**: The camera focuses on InkForm — now K-02 Shadow — at the centre of the Control Hub. The background shifts to show an endless sequence of new-generation children being guided through the Nurserie by K-01 units, their memories erased, their lives repeating identically.

The camera pulls back. The game returns to the **start menu** — with InkForm at the centre of the screen, menu buttons arranged around it. The same title screen the player saw when they first launched the game. The implication: Ending B is the loop. The game has already happened, infinite times, and will happen again. The only way to break the cycle is Ending A.

**End State**: Humanity survives as a species, but at the cost of everything that gave it meaning. The cycle of play begins again — literally.

---

## Collection System & Ending Influence

Throughout the game, destructible objects (glass tubes, cabinets, bookshelves) drop collectibles:

| Collectible Type | Effect |
|-----------------|--------|
| **Skill Shards** | Unlock new player abilities |
| **Key Items** | Unlock areas, trigger story events |
| **Documents** | Reveal lore; affect Arthur's dialogue in Chapter 3 |

**Documents** are the primary narrative lever. They are fragments of pre-war history, JARL internal memos, and personal writings by Arthur and Ruth. Examples:

- *Pre-War History Fragment*: Describes life before the Final War — families, art, culture.
- *JARL Internal Memo*: Debates about the Protocol's ethics (redacted).
- *Ruth's Personal Journal*: Conflicted thoughts about the project.
- *Arthur's Early Draft*: Earlier version of the Protocol with crossed-out sections showing his own doubt.

**Impact on Chapter 3 Dialogue**: If the player has collected certain documents, Arthur's dialogue in Chapter 3 changes. His arguments become less absolute. He acknowledges the documents as evidence of the world the Protocol is destroying. The choice is not locked — the player can still choose either ending — but the narrative framing shifts.

[TBD: Exact document list and dialogue variations — to be designed during Chapter 3 content creation.]

---

## Game Mechanics ↔ Narrative Summary

| Narrative Element | Game System | Status |
|------------------|-------------|--------|
| Ruth's audio guidance | S_AudioManager (spatialized audio) | Implemented |
| Player form-switching | S_Player (solid/fluid state machine) | Implemented |
| Destructible objects | S_BreakableObject (future) | Planned |
| K-01 patrols | S_NPCStory (fixed sequences) | Planned |
| Guards/chase enemies | S_NPCEnemy (patrol + chase state machine) | Planned |
| Dialogue scenes | S_NPCDialogue + S_DialogueUI | Planned |
| Suspicion meter | S_SuspicionSystem (new) | Planned |
| Document collection | S_InventorySystem (new) | Planned |
| Story triggers | S_GameEvent (OnStoryTrigger, OnArrestTriggered, etc.) | Partially implemented |
| Ending sequence | S_GameManager (scene loading + ending flag) | Planned |