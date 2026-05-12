# InkForm 项目简介

## 概述
Unity 2D 平台游戏。玩家控制一个可在固态与液态之间切换的史莱姆角色，通过形态切换解谜和探索关卡。关卡由升降平台组成，分段展现，逐步教学玩家跳跃、二段跳、冲刺和爬墙机制。

## 核心系统
| 系统 | 设计文档 | 状态 |
|------|----------|------|
| Player Controller | Player_Controller_Design.md | 已实现 |
| Skill System | Skill_System_Design.md | 已实现 |
| Level Section System | Level_Section_System_Design.md | 已实现 |
| Game Event System | Game_Event_System_Design.md | 已实现 |
| Manager Systems | Manager_Systems_Design.md | 已实现 |
| Level Objects | Level_Objects_Design.md | 已实现 |
| Moving Platform | Moving_Platform_Component_Design.md | 已实现 |
| NPC Guard System | NPC_System_Design.md | 已实现 |
| Suspicion System | Suspicion_System_Design.md | 已实现 |
| Player Procedural Rendering | Player_Procedural_Rendering_Design.md | 已实现 |
| Narrative System | Narrative_Design.md + Story_Outline.md + Characters.md | 设计中 |

## 核心玩法机制
| 机制 | 触发方式 | 状态 |
|------|----------|------|
| 固态/液态切换 | 输入切换 | 已实现 |
| 跳跃 | Jump 输入 | 已实现 |
| 二段跳 | MaxJump >= 2 | 已实现 |
| 冲刺 | Sprint 输入 + S_Soild_sprint 技能 | 已实现 |
| 冲刺击晕 | OverlapCircleAll 对敌方层 | 已实现 |
| 爬墙/爬天花板 | Grip 输入 + fluid 形态 + S_fluid_climb 技能 | 已实现 |
| 隐藏 | S_HideSpot + E 键切换 | 已实现 |
| NPC 逮捕 | Suspicion 满 100 或 3 任务完成 | 已实现 |
| 关卡分段 | SectionGoal 触发器 | 已实现 |

## 技术栈
- 引擎：Unity 6000.1.17f1 (Unity 6)
- 语言：C#
- 输入系统：Unity Input System (new) + S_InputBindingManager
- 渲染：URP 2D Renderer + S_PlayerProceduralRenderer
- 物理：Rigidbody2D (2D) + Dynamic Collider (Circle/Capsule)
- 版本控制：Git / GitHub (origin: https://github.com/pengzzzccc/Inkform-repo-v3.git)
- 项目管理：Jira (comp3150inkform.atlassian.net)