# 配置说明

## 位置与楼层

将 `Prefabs/Teleport/BH2VSQ_TeleportPoint.prefab` 或现有点复制到核心的 `Teleport` 子节点。每个点设置唯一 `locationId`、中文 `chineseName`、`floorId`、中文 `chineseFloorName`、`requiredRank`、`xpMultiplier` 和目的地 Transform。旧的英文名称字段保留供旧世界数据迁移，界面只显示中文。勾选 `tabVisible` 才在传送页列出；`radioDutyArea` 标记值守区。

**当前位置由区域碰撞体判定。** 调整每个点的 `AreaTrigger/BoxCollider`，勾选 `isTrigger`，让房间、走廊和出生区域被适当覆盖。系统每 0.75 秒检查本地玩家是否位于某个区域体积内，并同步位置。离开全部已配置区域时显示“未知”，人数统计也不再把该玩家计入旧区域。重叠区域取体积较小的一个。传送页每秒更新各点人数和当前所在点高亮。新增楼层只需新 `floorId`；运行时扫描核心下的点。`DefaultLocationDatabase.asset` 只是重新生成时使用的初始种子，不会覆盖已放入场景的实例。

核心根节点的 `BaseWorldSystem.returnUnauthorizedPlayersToSafePoint` 默认关闭：进入无权限区域不会被自动传回。若要启用，先将一个游客可达点标为 `isSafeFallback`，再在场景实例上勾选此选项。启用后，进入无权限点会传至安全点；找不到安全点时使用世界出生点。传送按钮始终执行权限检查。

## 楼层屏障

推荐使用 `Prefabs/Floor/BH2VSQ_FloorBarrier.prefab`。它是一个**专用的屏障父对象预制件**，不需要把屏障引用逐个配置到 `TeleportPoint`。

配置步骤：

1. 将 `BH2VSQ_FloorBarrier.prefab` 拖到核心 `Floor/BH2VSQ_FloorController` 对象的子节点下。
2. 在屏障父对象 Inspector 中只设置 `floorId`，使其与目标楼层一致。
3. 将你自己的屏障主体（模型、Collider、提示灯、粒子、特效等）直接拖成 `BH2VSQ_FloorBarrier` 的**直接子对象**。可以放多个子对象。

屏障父对象本身会始终保持激活，以便持续监听网络同步；父对象内部复用现有 `FloorManager` U# 程序，系统只切换它的直接子对象：

- `Open`（开放）：隐藏所有屏障子对象；
- `Reserved`（包场）：显示所有屏障子对象；
- `Maintenance`（维护）：显示所有屏障子对象。

这样一个楼层只需要一个屏障预制件，楼层有多个 `TeleportPoint` 也不需要重复绑定。屏障的状态来源仍然是现有楼层状态同步，晚加入玩家也会根据当前同步状态自动显示正确状态。

如果 `barrierFloorId` 无效、找不到父级 `FloorManager` 或楼层状态无法解析，系统会按**安全失败**处理并保持屏障主体显示，避免错误配置导致未授权入口暴露。


## 请求与认证

`Teleport/TeleportRequestManager.timeoutSeconds` 控制申请有效期，默认配置为 30 秒；旧场景实例可能保留原值。管理器可同时保存最多 16 条未过期请求，申请页每行显示发起者、发起者位置，以及绿色同意和红色拒绝按钮。收到请求时玩家会看到“按住 Tab 前往请求页”的短提示；发起者会收到结果提示。请求类型 0 为发起者传向接收者，类型 1 为邀请接收者传向发起者。

在**场景实例**的 `Authentication/TOTPAuthManager` 填写不同的成员和管理员 Base32 密钥；空值禁用对应等级。六位 HMAC-SHA1 TOTP 使用 30 秒周期，允许前后各一个周期。登录等级属于本地会话，重新加入后回到游客。`PlayerDataManager` 在 `OnPlayerRestored` 后读取经验和游戏时长，每 30 秒按当前位置倍率累计经验。界面只显示中文，旧语言偏好不会影响显示。

共享的玩家区域、楼层和请求数组在多人并发写入时仍需多客户端压力测试，详见[安全边界](SECURITY.md)。


### 楼层状态排除规则
1F（floorId=1）永久视为“开放”，不会出现在包场/维护管理列表，也不会被管理员切换为包场或维护；因此 1F 不需要配置屏障。

### 导入后编译一次

本项目包含修改后的 UdonSharp 源码。导入或替换资源后，请执行 `Tools → BH2VSQ BASE → Recompile UdonSharp Programs`，让 `FloorManager` 的 U# Program Asset 与最新源码同步。UdonSharp 官方文档也说明可以开启自动编译或全量编译脚本。
