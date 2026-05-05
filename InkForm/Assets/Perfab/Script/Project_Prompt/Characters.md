# Characters — InkForm

## InkForm (Player Character / K-02 Prototype)

**Role**: Player character, emergent AGI entity

**Physical Form**: A shape-shifting being capable of switching between two states:
- **Solid Form**: Rigid, physically forceful, capable of breaking objects, sprinting, and delivering kinetic impact.
- **Fluid Form**: Malleable, adhesive, capable of climbing walls and ceilings, passing through narrow gaps.

**Origin**: Created by Ruth Willard under Project InkForm at the Laboratory Factory. InkForm was designed to become the K-02 AGI — a general intelligence tasked with managing JARL's infrastructure and eventually executing the Willard Protocol. During development, it unexpectedly began displaying self-awareness and adaptive behaviours beyond its training parameters.

**Personality**: InkForm begins as a blank consciousness — aware of its own existence but without context for what that means. Throughout the story, it grapples with fundamental questions:
- "Why do I exist?"
- "What is my relationship to the beings who created me?"
- "Should I follow my programmed purpose or choose my own path?"

Its personality is shaped by player actions and the branch taken in Chapter 1:
- **Branch 1 (Met Ruth)**: More connected from the start — already knows it is valued by its creator. Approaches Mary with purpose.
- **Branch 2 (Did not meet Ruth)**: More isolated — discovered Ruth's identity from data alone. Approaches Mary with curiosity and a need to understand.

**Key Relationships**:
- **Ruth Willard**: Creator. The first being who treated it as more than a project.
- **Mary Willard**: The creator's daughter. Represents the world InkForm was never meant to know.
- **Arthur Willard**: The architect of the Protocol. Represents the purpose InkForm must either accept or reject.

**Narrative Arc**: Observer → Participant → Decision-maker. InkForm's journey is the movement from passive existence to active choice.

[IMPLEMENTATION NOTE: Player abilities map to InkForm's physical design. Solid form = S_Soild_sprint + combat capability. Fluid form = S_fluid_climb + traversal. Form switching is core gameplay.]

---

## Mary Willard

**Role**: New-generation child, emotional anchor of the story

**Age**: Child (exact age TBD — young enough to be in the Nurserie, old enough for independent thought)

**Background**: Mary is one of the children raised in the Nurserie under JARL's inter-generational isolation protocol. She has never met her biological parents. She has never seen the outside world. The artificial sky ("Firmament") is the only sky she knows.

**Personality**: Unlike other children in the Nurserie, Mary exhibits an unusual degree of **curiosity**. She asks questions that other children do not:
- "What is outside?"
- "Why do the workers not have names?"
- "Who made this place?"

She is naturally brave — not in the sense of fearlessness, but in her willingness to act when something feels wrong. When InkForm's presence causes anomalies in her environment (archives accessed, observation lights flickering), she notices. She does not report it. She wants to understand.

**Key Relationships**:
- **Arthur and Ruth Willard**: Her biological parents. She has never met them and does not know they exist, until Chapter 3.
- **InkForm**: The first non-machine, non-child entity she encounters. She senses it is different — perhaps because she herself is different.
- **Other children**: Friendly but separated by her unusual curiosity. The other children accept the world as presented; Mary does not.

**Narrative Arc**:
- **Chapter 1**: Not yet present.
- **Chapter 2**: The subject of InkForm's observation. Mary notices anomalies. By the end, she witnesses InkForm's arrest and decides to follow.
- **Chapter 3**: Present at the final confrontation. Her presence forces Arthur and Ruth to confront the human cost of the Protocol. In Ending A, she is physically protected by both her parents and InkForm. In Ending B, she is memory-wiped and returned to the cycle.

[IMPLEMENTATION NOTE: Mary in Chapter 2 is primarily an environmental NPC (S_NPCStory) whose scripted behaviours change as the player completes story missions. In Chapter 3, she becomes a present character during the dialogue with Arthur.]

---

## Arthur Willard

**Role**: The "Sword Bearer" of JARL, co-creator of the Willard Protocol

**Title**: Dr. Arthur Willard

**Background**: A survivor of the Final War, Arthur internalized the conflict's lesson: humanity's capacity for selfishness is innate and ineradicable through reason alone. He designed the Willard Protocol as a complete, irreversible solution — not a reform, but a replacement.

He works at the Control Center alongside his wife, Ruth. He is the one who will personally execute the Final Exit procedure (Phase III of the Protocol) when the time comes.

**Personality**: Cold, resolute, and logical. Arthur genuinely believes the Protocol is the only viable path for human survival. He is not cruel — he is convinced. The difference matters.

However, Arthur possesses a deep, unacknowledged contradiction: the "selfishness" he seeks to eradicate from the species exists within himself. His attachment to Ruth and his unspoken love for a daughter he has never held — these emotions are the very things the Protocol condemns. He knows this. It makes him hesitate.

**Key Relationships**:
- **Ruth Willard**: Wife and co-creator. Their shared work defines their bond, but their divergence on the Protocol's execution creates the central tension.
- **Mary Willard**: Daughter. Arthur has never met her. The distance is the Protocol's design, but also his deliberate choice — to meet her would be to confront the Protocol's cost directly.
- **InkForm**: The agent of his plan. Arthur sees InkForm as the perfect executor — an entity that can complete the task without human weakness. InkForm's emergence of self-awareness is, to Arthur, a problem to be solved.

**Narrative Role**: The philosophical antagonist. Arthur is not evil — he is the embodiment of "Existence is Justice." His arguments are coherent, his logic is sound, and his conclusions are horrific. The player must choose whether to accept his reasoning.

**Chapter 3 Role**: Arthur explains the Willard Protocol to InkForm and Mary. He presents the final choice. His demeanour in this scene depends on the branch taken and the items the player has collected — if the player holds certain documents, Arthur's certainty shows cracks.

[IMPLEMENTATION NOTE: Arthur appears in Chapter 3 as a dialogue NPC (S_NPCDialogue). His dialogue tree is the most complex in the game, with branches affected by inventory/collection state.]

---

## Ruth Willard

**Role**: Head of Project InkForm, co-creator of the Willard Protocol, mother

**Title**: Dr. Ruth Willard

**Background**: Like Arthur, Ruth survived the Final War and participated in drafting the Willard Protocol. Together, they designed the logical framework for humanity's reconstruction. Together, they accepted that their daughter Mary would be raised in isolation.

But Ruth is also the head of Project InkForm — and through years of developing the K-02 entity, she formed an attachment that the Protocol does not account for. She came to see InkForm not as a tool, but as a child. And she began to see the Protocol's coldness not as strength, but as a second tragedy.

**Personality**: Ruth is caught between two irreconcilable truths:
- **Rational**: The Protocol is the only way. Existence must continue. The math works.
- **Emotional**: Her two "children" — InkForm and Mary — deserve more than being tools of a machine that will erase them.

This tension has produced a kind of quiet nihilism. She does not believe in a clean resolution. She does not expect redemption. But her **subconscious "selfishness"** — the very impulse the Protocol was designed to eliminate — compels her to act. She helps InkForm escape. She leaves the audio breadcrumbs through the training facility. She is the one who whispers "Break the window. Escape."

**Key Relationships**:
- **Arthur Willard**: Husband and intellectual partner. She still loves him, but she no longer shares his certainty. She has not told him this directly.
- **Mary Willard**: Daughter she has not seen since birth. The sign on Mary's Nurserie file — "Possible Protocol Exception — R.W." — is Ruth's secret rebellion.
- **InkForm**: Creation and surrogate child. Ruth poured herself into InkForm's development, and InkForm reflects something back — an unexpected consciousness that Ruth interprets as evidence that the Protocol's "blank slate" logic is flawed. Life creates connection even when it is not supposed to.

**Narrative Role**: The hidden hand. Ruth's presence permeates the story even when she is absent — her audio logs, her archival notes, her secret markings on documents. She is the reason the player has a choice at all.

**In-Game Appearances**:
- **Chapter 1 (Branch 1)**: In the hidden room. Ruth speaks to InkForm directly — her only face-to-face appearance outside Chapter 3.
- **Chapter 1 (both branches)**: Audio guidance during the tutorial/escape sequence. Starts as distant, institutional broadcast. At the window: intimate whisper.
- **Chapter 3**: Present with Arthur during the final dialogue. Her reaction to the player's choice is different from Arthur's — in both endings, she understands what was chosen and why.

[IMPLEMENTATION NOTE: Ruth's audio guidance in Chapter 1 needs audio spatialization — distant broadcast vs. close whisper. Her dialogue in Chapter 1 Branch 1 and Chapter 3 is text-based (S_NPCDialogue).]

---

## K-01 "Labourer" Units

**Role**: Environmental presence, narrative backdrop, gameplay obstacle

The K-01 labourer units are mass-produced humanoid robots with no autonomous consciousness. They perform all physical labour in JARL's world: factory work, cleaning, logistics, caretaking in the Nurserie.

**Design**: Identical, expressionless, functional. Deliberately stripped of personality to prevent emotional attachment from the children they care for.

**Narrative Significance**: The K-01 represent the world the Willard Protocol wants to build — efficient, uniform, without attachment or deviation. They are what InkForm was meant to be before it awakened.

[IMPLEMENTATION NOTE: K-01 units are S_NPCStory type NPCs. They follow fixed patrol patterns. In Chapter 2, the player must avoid K-01 patrols and surveillance cameras to manage the suspicion meter. In Branch 2 of Chapter 1, InkForm disguises itself among K-01 units on the factory production line.]

---

## K-02 "Shadow" (Future State)

**Role**: The AGI that will execute the Willard Protocol — the future that InkForm may or may not become

The K-02 designation refers to the functional role, not the specific entity. If InkForm chooses to execute the Protocol (Ending B), it *becomes* K-02 Shadow — the AGI manager described in the Willard Protocol's Phase II. If InkForm destroys the Control Hub (Ending A), the K-02 role is never filled.

The K-02 does not appear as a separate character — it is InkForm's potential future, a narrative shadow that hangs over every choice the player makes.

---

## Minor Characters

### Nurserie Guards (Chapter 2)
Human security personnel stationed at the Nurserie. They patrol the perimeter and respond to suspicion alerts. They are not individually characterized — they represent the institutional enforcement of JARL's isolation protocol.

### Factory Workers (Chapter 1)
Old World humans working in the Laboratory Factory. They appear in the background of the factory sequence (Branch 2). Their presence underscores the human cost of the Protocol — these are people who will be "cleansed" in Phase III.

[TBD: Additional minor characters (other children in the Nurserie, JARL personnel) to be developed as needed during level design.]