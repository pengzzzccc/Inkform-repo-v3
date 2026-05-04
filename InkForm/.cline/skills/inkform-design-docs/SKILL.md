---
name: inkform-design-docs
description: Load InkForm game design documents from Project_Prompt directory. Use when implementing, modifying, or discussing any game system including player controller, skill system, level objects, moving platforms, mechanical claws, game events, manager systems, or level sections in Unity.
---

# InkForm Design Document Loading Skill

## Document Location
All design documents are located in `Assets/Perfab/Script/Project_Prompt/` under the project root.

## Document Index
Load only the document(s) relevant to the current task:

| Task Keywords | Document to Load |
|---------------|-----------------|
| Player, controller, movement, jump, solid, fluid, climb, form switch | Player_Controller_Design.md |
| Skill, skill tree, ScriptableObject, unlock | Skill_System_Design.md |
| Level objects, obstacles, mechanisms, interactable objects | Level_Objects_Design.md |
| Moving platform, elevator, mechanical claw, Claw | Moving_Platform_Component_Design.md |
| Event, event system, GameEvent, broadcast | Game_Event_System_Design.md |
| Manager, singleton, global state | Manager_Systems_Design.md |
| Level section, Section, platform rise/fall, level flow | Level_Section_System_Design.md |
| Changelog, history, updates | CHANGELOG.md |

## Usage Rules
1. Load only 1-2 relevant documents at a time — never read all at once
2. After loading, extract key information (state machine definitions, parameter tables, interface designs) — no need to memorize word-for-word
3. Implementation code must be consistent with the design document
4. When a design document needs updating, update the document first, then continue coding