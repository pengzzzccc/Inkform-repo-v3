# Level Flow Configuration Guide

本文档说明 InkForm 当前关卡流程系统如何配置、如何制作新关卡，以及 Inspector 中每个选项的含义。当前架构不保留旧线性关卡流；所有运行流程都从 `S_RunFlowConfig` 读取。

## 1. 总览

运行时分三层：

- `S_GameManager`：只负责场景加载、淡入淡出转场、输入锁、帧率和退出游戏。
- `S_RunFlowController`：只负责流程判断，包含固定训练、随机训练、设施房间图、结局。
- `S_RunFlowConfig`：唯一的关卡流程配置入口。设计师主要修改这个 asset。

当前默认配置文件：

- `Assets/Perfab/Configs/Levels/RunFlowConfig.asset`
- `Assets/Perfab/Configs/Levels/Training/*.asset`
- `Assets/Perfab/Configs/Levels/Facility/RoomGraph.asset`

开始菜单点击 start 后触发 `RunStartRequested`，`S_RunFlowController` 读取 `RunFlowConfig` 并加载第一个固定训练关。

## 2. RunFlowConfig

创建方式：

1. 在 Project 窗口右键。
2. 选择 `Create > InkForm > Level Flow > Run Flow Config`。
3. 将 asset 绑定到 `ManagerRoot.prefab` 下的 `RunFlowController`。

字段说明：

| 字段 | 含义 | 推荐/注意 |
| --- | --- | --- |
| `Start Menu Scene` | 返回菜单时加载的场景 | 当前为 `Assets/Scenes/For_game/Start.unity` |
| `Ending Scene` | 进入结局时加载的场景 | 当前为 `Assets/Scenes/For_game/END.unity` |
| `Fixed Training Levels` | 新游戏开始后按顺序播放的训练关列表 | 当前为 `Train 1` 到 `Train 5` |
| `Random Training Pool` | 固定训练结束后随机抽取的训练关池 | 当前为 `Train 3`、`Train 5`、`Train 6` |
| `Min Random Training Rooms` | 每轮随机训练最少抽取数量 | 当前为 `2` |
| `Max Random Training Rooms` | 每轮随机训练最多抽取数量 | 当前为 `3`，不能超过池子数量 |
| `Room Graph` | 设施阶段的房间拓扑配置 | 当前为 `RoomGraph.asset` |
| `Dr Room Ending Story Id` | 某个剧情 trigger 直接进入结局的 id | 当前为 `Dialogue_End_DrR` |

`Fixed Training Levels` 与 `Random Training Pool` 中每个元素都是 `S_LevelSceneEntry`：

| 字段 | 含义 | 推荐/注意 |
| --- | --- | --- |
| `Id` | 稳定关卡 id，用于人读和调试 | 推荐小写加数字，如 `train_01` |
| `Display Name` | Inspector/debug 中显示的名称 | 可写 `Train 1` |
| `Level Kind` | 关卡类型 | 固定训练用 `FixedTraining`，随机训练用 `RandomTraining` |
| `Scene` | 要加载的 Unity 场景 | 必须加入 Build Settings / Build Profiles |
| `Training Config` | 训练关内部教程配置 | 只有训练关需要填写 |

错误配置表现：

- `Start Menu Scene` 空：返回菜单会输出 warning，不会跳转。
- `Ending Scene` 空：进入结局会输出 error，不会跳转。
- 随机训练池为空：固定训练结束后直接进入设施阶段。
- 随机数量为 0：直接进入设施阶段。
- `Room Graph` 空或没有入口房间：设施阶段无法开始，并输出 error。

## 3. TrainingLevelConfig

创建方式：

1. 右键 `Create > InkForm > Level Flow > Training Level Config`。
2. 保存到 `Assets/Perfab/Configs/Levels/Training/`。
3. 将它绑定到训练场景中的 `TutorialController`，并在 `RunFlowConfig` 的对应 `S_LevelSceneEntry` 中引用。

字段说明：

| 字段 | 含义 | 推荐/注意 |
| --- | --- | --- |
| `Tutorial Type` | 教程开场模式 | `None` 跳过语音和提示；`TeachAndPractice` 播放熟悉操作语音、显示操作面板并等待输入；`PracticeOnly` 跳过操作教学，只播倒计时提示 |
| `Skills To Unlock` | 本训练关可用技能名 | 字符串必须匹配技能名，如 `Sprint`、`FluidClimb`、`CameraControl` |
| `Familiarize Voice Clip` | 熟悉操作阶段语音 | 可为空；为空时只显示字幕/流程继续 |
| `Familiarize Subtitle` | 熟悉操作字幕 | 例如 `Now get familiar with the controls.` |
| `Countdown Digit Voice Library` | 数字拼接语音库 | 用于根据 `Time Limit` 自动拼接倒计时语音 |
| `Auto Generate Countdown Subtitle` | 是否根据时间自动生成倒计时字幕 | 推荐开启，避免字幕和时间不一致 |
| `Countdown Subtitle` | 手写倒计时字幕 | 仅在自动生成关闭或文本为空时影响显示 |
| `Required Actions` | 教学面板必须完成的输入清单 | 为空时按任意键关闭提示 |
| `Prompt Title` | 教学面板标题 | 例如 `Sprint Controls` |
| `Prompt Description` | 教学面板正文 | 建议列出按键和用途 |
| `Time Limit` | 倒计时时长，秒 | `0` 或小于等于 0 时不启用倒计时，只等待通关 |
| `Startup Delay` | 场景加载后等待多久开始教程流程 | 用于等待玩家/相机初始化 |
| `Pan To Target Duration` | 镜头移动到目标点耗时 | 目标点由场景里的 `CameraPanTarget` 指定 |
| `Pan Hold Duration` | 镜头停留在目标点时间 | 让玩家看清目标位置 |
| `Pan Return Duration` | 镜头返回玩家耗时 | 设为 0 可瞬间返回 |
| `Pre Countdown Delay` | 镜头返回后，倒计时开始前的额外等待 | 用于节奏调试 |
| `Timeout Death Delay` | 倒计时失败后，触发死亡前的等待 | 让字幕/UI 有时间收尾 |
| `Has NPC` | 标记该关是否有 NPC | 当前主要给设计师和编辑器工具参考 |
| `Is Last Fixed Tutorial` | 旧流程留下的设计标记 | 新流程不依赖它；是否为最后固定训练由 `RunFlowConfig.Fixed Training Levels` 顺序决定 |

`Required Actions` 子字段：

| 字段 | 含义 |
| --- | --- |
| `Action Name` | UI 清单显示名称，如 `Move`、`Jump` |
| `Keyboard Keys` | 完成该动作的键盘按键名，如 `W`、`A`、`Space` |
| `Gamepad Keys` | 完成该动作的手柄按键名，如 `ButtonSouth`、`LeftStick` |

## 4. CountdownDigitVoiceLibrary

文件位置：

- `Assets/Perfab/Tutorial/CountdownDigitVoiceLibrary.asset`

字段说明：

| 字段 | 含义 |
| --- | --- |
| `Prefix Clip` | 倒计时语音前缀，如 `Reach the goal within` |
| `Seconds Clip` | 结尾单位语音，如 `seconds` |
| `Digit Clips` | 0 到 9 的数字语音，数组下标必须对应数字 |
| `Clip Gap` | 多段语音之间的间隔 |

如果某个数字 clip 缺失，`S_TutorialController` 会只显示字幕并输出 warning。

## 5. RoomGraph

创建方式：

1. 右键 `Create > InkForm > Room Graph`。
2. 保存到 `Assets/Perfab/Configs/Levels/Facility/`。
3. 将 asset 填到 `RunFlowConfig.Room Graph`。

字段说明：

| 字段 | 含义 | 推荐/注意 |
| --- | --- | --- |
| `Rooms` | 房间节点数组 | 每个正式设施房间一个节点 |
| `Id` | 房间枚举 id | 来自 `RoomId`，如 `ComR`、`PS`、`BF`、`LivA`、`For` |
| `Scene` | 该房间对应场景 | 必须加入 Build Settings / Build Profiles |
| `Adjacent Rooms` | 可直接到达的相邻房间 | 系统按无向图处理，只要一侧写了相邻即可 |
| `Is Facility Entry` | 是否可作为随机训练后的设施入口 | 当前 `ComR` 和 `BF` 为入口 |
| `Is For Ending` | 进入该房间是否直接走结局 | 当前 `For` 会进入 `END` |

房间出口 prefab 使用 `S_RoomExit.targetRoom` 指向目标房间。运行时 `S_RunFlowController` 会校验目标房间是否和当前房间相邻；非相邻请求会被拒绝并输出 warning。

## 6. 常用 Prefab

| Prefab / Component | 用途 | 关键配置 |
| --- | --- | --- |
| `ManagerRoot.prefab` | 持久化管理器根节点 | 必须放在 `Start.unity` 和可直接打开测试的场景根部 |
| `S_RunFlowController` | 运行流程控制器 | 绑定 `RunFlowConfig.asset` |
| `Pre_TutorialController.prefab` | 训练关教程流程 | 场景实例要绑定 `TrainingLevelConfig` 和 `CameraPanTarget` |
| `Pre_ExitDoor.prefab` / `S_ExitGate` | 钥匙门，通关出口 | `Required Keys` 决定需要几把钥匙；解锁后触碰触发 `LevelCompleted(Goal)` |
| `Pre_InkPod_Entry.prefab` / `S_InkPod` | 进入舱，播放入舱动画后完成关卡 | `Mode` 必须是 `Entry` |
| `Pre_InkPod_Spawn.prefab` / `S_InkPod` | 出生舱，场景开始时把玩家放出来 | `Mode` 必须是 `Spawn` |
| `Pre_Checkpoint.prefab` / `S_Checkpoint` | 保存当前场景出生点 | 玩家触碰后发送 `SpawnPointChanged` |
| `S_SceneCheckpointTracker` | 每场景自动创建的 checkpoint 追踪器 | 死亡 UI 点击 restart 后响应 `RespawnRequested` |
| `S_RoomExit` | 设施房间出口 | `Target Room` 必须是相邻房间 |
| `S_EndingTrigger` | 结局触发器 | 玩家进入后发送 `EndingRequested` |
| `S_SectionGoal` | 分段关卡 start/end 触发器 | `Complete Level On End` 为 true 时，End trigger 会完成关卡 |

## 7. 制作新训练关

1. 复制一个现有训练场景，例如 `Train 3.unity`。
2. 重命名并保存到 `Assets/Scenes/For_game/Training Room/`。
3. 创建对应 `TrainingLevelConfig`，保存到 `Assets/Perfab/Configs/Levels/Training/`。
4. 打开训练场景，选中 `TutorialController`，绑定新的 `TrainingLevelConfig`。
5. 设置或移动 `CameraPanTarget` 到目标展示位置。
6. 确认终点 `S_SectionGoal` 的 `Trigger Type` 为 `End`，并勾选 `Complete Level On End`。
7. 将场景加入 Build Settings / Build Profiles。
8. 如果是固定训练，加入 `RunFlowConfig.Fixed Training Levels`。
9. 如果是随机训练，加入 `RunFlowConfig.Random Training Pool`。

## 8. 制作新设施房间

1. 新建或复制一个设施场景，保存到 `Assets/Scenes/For_game/Lab Room/`。
2. 确保场景里有玩家、相机、ManagerRoot、出生点或 Spawn InkPod。
3. 在 `RoomId.cs` 中添加新的房间 id。
4. 在 `RoomGraph.asset` 的 `Rooms` 中添加节点，绑定 scene。
5. 填写 `Adjacent Rooms`，决定它和哪些房间连通。
6. 在场景内放置 `S_RoomExit`，将 `Target Room` 设为要去的相邻房间。
7. 将场景加入 Build Settings / Build Profiles。
8. 进入 Play Mode，从相邻房间出口测试跳转。

## 9. 制作结局房间

有两种方式：

- 在 `RoomGraph` 中把某个房间节点勾选 `Is For Ending`。玩家请求进入该房间时，流程直接加载 `Ending Scene`。
- 在场景中放置 `S_EndingTrigger`。玩家进入触发器后，流程加载 `Ending Scene`。

如果需要剧情触发结局，发送 `S_GameEvent.StoryTrigger(triggerId)`，并让 `triggerId` 等于 `RunFlowConfig.Dr Room Ending Story Id`。

## 10. 验证清单

- `RunFlowController` 已绑定 `RunFlowConfig.asset`。
- 所有 `S_SceneReference` 场景都在 Build Settings / Build Profiles 中启用。
- 固定训练列表顺序正确。
- 随机训练池数量大于等于 `Max Random Training Rooms`。
- `RoomGraph` 至少有一个 `Is Facility Entry` 房间。
- 每个 `S_RoomExit.targetRoom` 都是当前房间的相邻房间。
- 死亡 UI 的 restart 能触发 checkpoint respawn。
- 项目搜索不到旧流程事件名。
