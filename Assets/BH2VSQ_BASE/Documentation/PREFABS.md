# 预制组件用途与实际场景配置

正式世界只需放入**一个** `Prefabs/Core/BH2VSQ_BASE_Core.prefab`。它包含管理器、中文控制台、请求提示与初始传送点。下面的零件预制体供改造核心或自定义界面使用；不要把整套零件再次放入同一场景，否则会产生重复管理器与同步对象。

## 最小安装

1. 导入资源包，在已有 VRChat 世界场景中放入一个核心实例；确保场景只有一个有效 `EventSystem`。核心自带一个，旧场景若已有一个，请禁用或移除重复的实例。
2. 将核心 `Teleport/Point_*` 放到实际房间，设置中文地点/楼层名、权限、目的地和 `AreaTrigger/BoxCollider`。碰撞体应覆盖玩家能行走的实际区域；未覆盖处显示“未知”。新增地点时在同一 `Teleport` 子节点复制 `BH2VSQ_TeleportPoint.prefab`，设置唯一 `locationId`，无需修改代码。重叠区域优先取较小体积。
   需要物理/视觉屏障时，推荐在核心 `Floor/BH2VSQ_FloorController` 下放置 `Floor/BH2VSQ_FloorBarrier.prefab`，只设置 `barrierFloorId`（楼层 ID）；无需配置隐藏的 `barrierController` 开关，再把自定义屏障主体直接拖成该父对象的子节点。屏障会自动从所在核心层级寻找 `TeleportManager`，不需要再手动连接传送管理器。楼层开放时所有子节点自动隐藏，包场或维护时自动显示；同楼层多个点不需要重复绑定。正常 `FloorManager` 会自动刷新其直接子节点中的屏障控制器，网络同步后的状态变化也会更新屏障。
3. 在核心根节点 Inspector 设置 `BaseWorldSystem.menuOpacity`（0～1）。桌面端**按住 Tab 显示、松开隐藏**；VR 玩家可通过世界交互按钮调用 `BaseWorldSystem.ToggleMenu`。核心的 `returnUnauthorizedPlayersToSafePoint` 默认关闭；若要自动传回，请先指定唯一游客可达的 `isSafeFallback` 点再开启。
4. 仅在场景实例的 `Authentication/TOTPAuthManager` 填成员和管理员 Base32 密钥。验证配置，随后在 ClientSim 检查 UI，并用多客户端检查同步。不要把世界内密钥用于真实身份或付费权限。

主菜单位于视野左侧，采用蓝色半透明控制台布局。顶部始终显示玩家名、权限、等级、经验进度及游戏时长，右侧仅有 TOTP 输入框和确认按钮。下方四个图标标签分别是传送、玩家、申请、管理；当前标签高亮，管理标签只对管理员显示。传送按钮直接移动玩家，显示该区域人数并高亮当前位置。玩家列表和详情自动刷新，无需手动刷新。申请页可处理最多 16 条同步请求，行内绿色对勾/红色叉分别为同意/拒绝；顶部短提示提醒接收者按住 Tab 处理申请，发起者收到结果提示。

`Data/Default/DefaultLocationDatabase.asset` 是重新生成核心时的初始点种子；修改它不会更新已经放入场景的实例。`Data/Localization/DefaultLocalization.asset` 仅包含中文界面文本。旧版本世界中的英语名称字段仍可用于数据迁移，但界面只显示中文。

## 核心层级

| 子节点 | 职责 | 常用配置 |
| --- | --- | --- |
| `Core` | 玩家登记、经验/时长持久化、每 0.75 秒检测本地位置 | 位置检测依赖每个点的区域碰撞体。 |
| `Authentication` | TOTP 本地会话与游客/成员/管理员等级 | 密钥仅在场景实例设置。 |
| `Floor` | 楼层状态、区域人数、电台值守 | 楼层 ID 和权限在点上配置。 |
| `Teleport` | 位置点、直接传送和传送申请 | 调整点、触发器和申请超时。 |
| `Admin` | 管理员权限与楼层状态 | 由管理标签操作。 |
| `UI` | 顶部资料卡、标签页、请求提示 | 保留主菜单上的交互画布组件；请求短提示画布不接收点击。 |

## 每个预制体

以下“单独使用”说明适用于定制开发。导出时会清除零件指向核心外部对象的引用，必须按 Inspector 字段重新连接；通常应直接修改完整核心实例。

| 预制体 | 用途 | 单独使用时的关键连接 |
| --- | --- | --- |
| `Core/BH2VSQ_BASE_Core.prefab` | 完整系统，含所有管理器、UI 和初始点 | 正式场景只放一个，配置点位、密钥、出生点及 EventSystem。 |
| `Demo/BH2VSQ_DemoBase.prefab` | 演示核心副本 | 只用于检查；不要与正式核心并存。 |
| `Teleport/BH2VSQ_TeleportPoint.prefab` | 一个传送目的地和区域 | 放在核心 `Teleport` 下，设置 ID、中文名称、楼层、等级、目的地与碰撞体；楼层屏障请使用 `Floor/BH2VSQ_FloorBarrier.prefab`。 |
| `Floor/BH2VSQ_AreaTrigger.prefab` | 进入区域时记录位置；可选无权限自动传回 | 放在对应点下，保持 `BoxCollider.isTrigger`，连接 `point`、`tracker`、`access`、`teleport`、`world`；点预制体已自带一个。 |
| `Floor/BH2VSQ_FloorController.prefab` | 按点汇总楼层及状态 | 连接 `TeleportManager`，不要与原有实例并存。屏障预制件应放在它的子节点下。 |
| `Floor/BH2VSQ_FloorBarrier.prefab` | 楼层包场/维护屏障父对象 | 只设置 `barrierFloorId`；无需配置隐藏的 `barrierController` 开关。把自定义屏障主体直接作为子对象放入。父对象应放在普通 `BH2VSQ_FloorController` / `FloorManager` 对象的子层级中。屏障父对象本身始终保持 Active，只切换其直接子对象。该预制件复用 `FloorManager` 的 U# Program Asset，不新增独立 UdonSharp 脚本。 |
| `Floor/BH2VSQ_AreaController.prefab` | 位置名称与经验倍率查询 | 连接 `TeleportManager`。 |
| `UI/BH2VSQ_TabMenu.prefab` | 顶部资料卡及四个标签页 | 放入带 `VRCUiShape`、`BoxCollider`、`GraphicRaycaster` 的 World Space Canvas，连接页、权限、按钮与高亮图标。 |
| `UI/BH2VSQ_PersonalInfo.prefab` | 顶部玩家资料与经验条 | 连接玩家数据、权限和中文文本；完整核心中始终显示于菜单顶部。 |
| `UI/BH2VSQ_PermissionLogin.prefab` | 六位 TOTP 输入和确认按钮 | 连接 `TOTPAuthManager`、菜单和请求提示服务。 |
| `UI/BH2VSQ_Teleport.prefab` | 直接传送、人数及当前位置高亮 | 连接 `TeleportManager`、`PlayerAreaTracker`、`AreaPopulationManager`。 |
| `UI/BH2VSQ_PlayerList.prefab` | 自动更新的玩家列表和右侧详情 | 连接登记、位置、玩家数据、详情及中文文本。 |
| `UI/BH2VSQ_PlayerListItem.prefab` | 玩家列表的行按钮 | 由玩家列表复用，`UIButtonAction` 连接详情并设置玩家 ID。 |
| `UI/BH2VSQ_PlayerDetail.prefab` | 选中玩家的位置、等级及两个申请按钮 | 连接玩家登记、位置与 `TeleportRequestManager`。 |
| `UI/BH2VSQ_TeleportRequest.prefab` | 申请列表页面 | 与 `RequestPanel`、请求管理器和申请按钮池连接；完整核心已接好。 |
| `UI/BH2VSQ_RequestService.prefab` | 收件提示与申请列表控制器 | 连接申请页、区域信息、管理器和独立的短提示画布。 |
| `UI/BH2VSQ_AdminPanel.prefab` | 楼层状态和区域人数控制台 | 连接管理员、楼层、区域人数及电台组件。 |
| `UI/BH2VSQ_FloorAdmin.prefab` | 开放/包场/维护状态操作区 | 通过 `FloorAdminManager` 修改楼层状态。 |

## 故障排查与升级

- **按住 Tab 无显示**：检查核心 `BaseWorldSystem`、`menuCanvas`、`menuFollower` 引用及游戏窗口焦点。松开 Tab 后菜单消失是预期行为。
- **按钮无法点击**：主菜单 Canvas 需要 `VRCUiShape`、`BoxCollider`、`GraphicRaycaster`，位于 Default 图层；场景仅留一个有效 `EventSystem`，并检查玩家与菜单之间有无实体碰撞体遮挡。
- **位置一直未知或人数不更新**：把对应点的区域碰撞体扩展至玩家实际行走的位置，检查碰撞体已启用及点的 ID 唯一。打开菜单后每秒刷新显示。
- **旧场景仍看到个人页或旧提示**：重新导入新版资源包不会自动改动已放入其他场景的旧实例。先记录旧点位、触发器和密钥，再替换为新版核心并重新配置。
- **单独零件没有反应**：零件没有自动连接完整核心；按上表接回引用后运行 **BH2VSQ BASE → 验证配置**。


### 楼层状态排除规则
1F（floorId=1）永久视为“开放”，不会出现在包场/维护管理列表，也不会被管理员切换为包场或维护；因此 1F 不需要配置屏障。

### 导入后编译一次

本项目包含修改后的 UdonSharp 源码。导入或替换资源后，请执行 `Tools → BH2VSQ BASE → Recompile UdonSharp Programs`，让 `FloorManager` 的 U# Program Asset 与最新源码同步。UdonSharp 官方文档也说明可以开启自动编译或全量编译脚本。
