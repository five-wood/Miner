# 会话日志 v2 设计

日期：2026-08-16  
状态：已确认，实现计划见 `docs/superpowers/plans/2026-08-16-session-log-v2.md`

## 1. 背景

现有日志是每秒一行的宽表（`CombatMgr.Record` + `XLogger.Record` + `RecordUtils` 逗号拼接）。采集分析时有三个问题：

1. **单元格打包**：同一秒多次结算时，`Avatar_healthchange` / `Avatar_goldchange` 把多个值用逗号拼进一个单元格，解析容易出错。
2. **无法识别 agent 类型**：类型没有直接写进日志；被 block 且没有数值结算时，事后无法推断类型。
3. **时钟漂移**：`MainView` 用 `recordDuration > 1` 后把余数清零，大约每 97 秒偏 1 秒；游戏机与传感器电脑的系统时钟也未同步，事后对齐困难。

本设计把正式采集日志改成 **long 表、每秒骨架**，一次游玩一个文件。旧宽表不再用于正式采集。

## 2. 目标与非目标

**目标**

- 每个在场 agent 每秒至少一行，带 `spawn_id` 和 `agent_type`。
- 任何单元格只含一个值，禁止逗号拼接。
- 玩家动作与 agent 事件分开；block 成功也带 agent 身份。
- 每关有 `level_start` / `level_end` 时间戳，普通行只用从 1 起的 `second`。
- `second` 不再因余数丢弃而漂移。

**非目标**

- 不改 hook / block / 碰撞 / Threat 射击的玩法手感（结算数值保持现状）。
- 不在运行时生成或改写 `spawn_id`。
- 本轮不实现被试编号输入；`participant_id` 列保留但留空。
- 不保留旧宽表列（facing、nearest、inrange 等）。

## 3. 架构

采用 **秒内事件队列 + 每秒刷 long 表**。

```
游戏事件（Catch / OnSuccessCatch / BeHurt / ...）
        ↓
  SessionLogger.Enqueue(结构化事件)
        ↓
  每满 1 个游戏秒 → FlushSecond()
        ↓
  写出：level_start? + 无事件在场行 + 事件行 + 空秒行? + level_end?
```

**职责划分**

| 单元 | 职责 |
|---|---|
| `SessionLogger` | 建文件、写 schema 头、维护本关秒计数、事件队列、已出现的 spawn_id、刷行、关卡起止时间戳 |
| `AgentConfig.spawnId` | 从 `cfg.csv` 的 `spawn_id` 列读入，原样挂到实体上 |
| 现有战斗逻辑 | 在既有调用点入队，不在日志器里重做碰撞判定 |
| `CombatMgr.Record` / `RecordUtils` 拼接 | 停止用于正式采集 |

`SessionLogger` 只依赖：当前玩家血量/金币、在场 agent 列表（含 config、位置）、本秒事件队列。不依赖 UI。

## 4. 文件与表头

每次启动游戏生成一个独立 CSV（沿用现有 `Assets/log_{date}_{time}.csv` 命名）。

第一行：

```
# schema_version=v2
```

第二行是列名，顺序固定：

```
second,participant_id,level,wave,spawn_id,agent_type,n_reward,n_threat,n_coactive_fat,n_coactive_tall,event_type,avatar_hp_delta,avatar_gold_delta,avatar_hp,avatar_gold,unix_timestamp,datetime,in_field,distance,warning
```

约定：

- 数值列输出数字；是否类用 `0/1`。
- 空值就是空单元格，不写 `none`、`-`、多余逗号或尾部空格。
- `participant_id` 整列留空，本轮不做输入来源。

## 5. 列定义

| 列 | 类型 | 何时有值 | 说明 |
|---|---|---|---|
| second | int | 所有行 | 本关第几秒，从 1 起，每关重置 |
| participant_id | string | 本轮恒空 | 预留被试编号 |
| level | int | 所有行 | 关卡编号 |
| wave | int | agent 行 | 该 agent 配置中的波次；玩家动作 / 空秒 / level_start / level_end 留空 |
| spawn_id | string | agent 行 | 配置表原样透传；玩家动作和空秒留空 |
| agent_type | string | agent 行 | `Reward` / `Threat` / `Coactive_Fat` / `Coactive_Tall` |
| n_reward 等四列 | int | 所有普通行 | 本秒曾经在场的各类型数量（含本秒离场者）；同一秒各行相同；level_start / level_end 也填当秒值 |
| event_type | string | 有事件时 | 见第 6 节；无事件留空 |
| avatar_hp_delta | int | 所有行 | 有符号；无变化为 0 |
| avatar_gold_delta | int | 所有行 | 有符号；无变化为 0 |
| avatar_hp | int | 所有行 | 本行 delta 结算后的血量 |
| avatar_gold | int | 所有行 | 本行 delta 结算后的金币 |
| unix_timestamp | int64 | 仅 level_start / level_end | 游戏机系统时钟，epoch 毫秒 |
| datetime | string | 仅 level_start / level_end | `yyyy-MM-dd HH:mm:ss`（24 小时） |
| in_field | int | 所有行 | agent 在场行（含离场事件那一行）为 1，其余为 0 |
| distance | float | agent 在场行 | 该 agent 当秒到玩家的距离；其他行留空 |
| warning | string | 重复进场时 | 同一会话中该 `spawn_id` 再次用于 `entry` 时写 `duplicate_spawn_id`，否则留空 |

`agent_type` 按实体种类映射，不读配置表里的 `Agent Type` 组合角色列：

| 实体 | agent_type | n_* 计数列 |
|---|---|---|
| Lucky grass | Reward | n_reward |
| Toxic Vine | Threat | n_threat |
| Fat Mushroom | Coactive_Fat | n_coactive_fat |
| Tall Mushroom | Coactive_Tall | n_coactive_tall |

## 6. 事件类型

分两类。玩家动作没有 `spawn_id` / `agent_type` / `wave`。agent 事件有。

### 6.1 玩家动作

| event_type | 入队点 | 含义 |
|---|---|---|
| hook_fire | `Player.Catch` 真正发出 hook 时 | 不知道会打中谁 |
| hook_miss | hook 飞满行程未抓到 | 无目标 |
| block_miss | 盾飞满行程未挡到 | 无目标 |

成功 block **不**写 `block_fire`，只写一行 agent 事件 `block`。

hook 飞行途中不能再发射 hook 或 block（现有 `IsCasting` 已保证）。飞行期间目标 agent 继续写在场行，直到 `hook_hit` 或其它离场事件。

### 6.2 agent 事件

| event_type | 入队点 | 离场？ | 数值（按现有游戏） |
|---|---|---|---|
| entry | `CombatMgr.CreateItem` 之后 | 否 | 0 / 0 |
| shot | `Player.BeHurt`（子弹命中、掉血发生时） | 否 | 现为 hp −5，gold 0 |
| hook_hit | `Player.OnSuccessCatch`（钩子碰到 agent） | 是，最后一行 | 现有抓住结算（`GenerateHp` / `GeneratePoint`） |
| block | `CombatMgr.OnProtectSuccess` | 是，最后一行 | 0 / 0 |
| collision | `Player.OnHit` 且不是被 hook 拉回 | 是，最后一行 | hp 0，gold −30 |

没有单独的离场事件。`hook_hit` / `block` / `collision` 本身就是该 agent 的最后一行。

### 6.3 hook_hit 结算时点

现有代码在 agent **被拉回玩家身边**时才走 `CommonHitHandler`。日志的 `hook_hit` 记的是 **钩子碰到 agent** 的那一秒，因此：

- 抓住带来的血量/金币变化提前到 `OnSuccessCatch` 执行，并记在 `hook_hit` 行上。
- 该 agent 此后不再写在场行（视觉上仍可被拉回）。
- 拉到玩家身边时不再重复加减血/金币。

这只移动结算时点（通常提前不到 1 秒），不改变加减数值。

### 6.4 block 与击飞

`OnProtectSuccess` 后 agent 仍会击飞约 3 秒再销毁。日志上 `block` 即为离场：打上已离场标记，击飞期间不再写在场行。

### 6.5 collision 数值

维持 2026-01-19 规则：**被动碰撞金币 −30，血量不变**。实例表里的 −30 血只是格式演示，不以它改玩法。

## 7. 每秒刷行规则

`FlushSecond()` 在本关第 N 秒结束时调用，`second = N`。

**在场定义**：尚未被标记离场、且不是玩家的 agent。`hook_hit` / `block` / `collision` 入队时立即标记离场，本秒仍为这些事件各写一行（`in_field = 1`），之后秒不再出现。

**本秒计数 `n_*`**：本秒曾经在场的各类型数量，包括本秒进场和本秒离场者。同一秒所有行（含玩家动作行、空秒行）填同一组计数。

**同一秒写入顺序（显式规定，避免和实例表个别行序歧义）：**

1. 若本秒是本关第一秒：先写 `level_start`（`second = 1`，带时间戳）。
2. 本秒在场、且没有任何事件的 agent：各写一行在场记录（`event_type` 空，delta 为 0），按进场时间升序。
3. 按入队先后写出本秒所有事件行。同一 agent 多事件（例如先 `shot` 再 `block`）就写多行，不合并单元格。
4. 若步骤 1–3 都没有行：写一行空秒（agent 相关列空，`in_field = 0`，玩家状态照填）。
5. 若本关在本秒结束：最后写 `level_end`（带时间戳）。

步骤 2 把无事件在场行放在秒首。实例表第 5 秒把无事件的 FAT_08 写在 `hook_hit` 之后；实现不跟那一行的次序，因为无事件行 delta 为 0，分析应以事件行序还原结算先后。第 7 秒实例（先写 FAT 在场再写 `shot`）与本规则一致。

**血量/金币逐行更新**：刷行时用本秒开始时的玩家血量/金币作起点，每写一行加上该行 delta，写出结算后的值。游戏内结算在事件发生时已经生效；刷行只是按同一顺序回放 delta，不二次改玩家状态。

实现上事件入队时快照该事件的 delta，以及当时的 `distance`（agent 事件）。无事件在场行的 `distance` 用刷行时的当前位置。

## 8. 时间与关卡生命周期

### 8.1 second

- 整数，从 1 开始，每关重置。
- 用本关游戏进行时间的整秒，不用全局自增、不在 `> 1` 后把余数清零。
- 累加器写法：`elapsed += deltaTime`；当 `elapsed >= 1` 时 `FlushSecond()`，然后 `elapsed -= 1`（保留余数）。
- 由 `CombatMgr.UpdateGame` 在 `IsPlayingGame() == true` 时累加并触发 flush。暂停、死亡结算、胜利结算期间都不累加。不再由 `MainView` 在开局后无条件每秒调用 `Record()`。

### 8.2 关卡起止

- `LoadGame` / 开局：重置秒计数；`level_start` 出现在第一秒 flush 的第一行。时间戳在 `LoadGame` 时采样并缓存，避免 flush 晚于开局将近 1 秒导致锚点偏移。
- `level_end` 在「玩法结束且不会续关」时写：胜利（`OnGameOver` 且 `isWin == true`）、主动退出回主界面、退出游戏。时间戳取该瞬间。死亡后 5 秒续关不写 `level_end`。
- 胜利后结算界面不再累加 `second`；`level_end` 落在最后一秒玩法的 flush 末尾，不按结算界面停留时间延后。
- `level_start` / `level_end` 的 `wave`、`spawn_id`、`agent_type` 留空；`unix_timestamp` 与 `datetime` 只在这两类行填写。

### 8.3 死亡续关

现有流程：死亡 → `OnGameOver` → 约 5 秒后 `ContinueGame`（时间拨回当波，清场重生）。

- 不写 `level_end`，不重置 `second`。
- 结算界面期间不累加、不刷行。
- `ContinueGame` 之后从当前 `second` 继续。
- 拨回的 `gameTime` 只影响刷怪，不影响日志秒计数。

## 9. spawn_id

- 来源：`cfg.csv` 的 `spawn_id` 列。配置方保证格式 `L{level}_W{wave}_{TYPE}_{nn}`（TYPE 为 `RWD` / `THR` / `FAT` / `TALL`），全局唯一、与该行 level / wave / 类型一致。
- 游戏：`BaseConfig.InitAllLevel` 读入到 `AgentConfig.spawnId`；缺列或空值时写空字符串，并在启动时打一次警告，不崩溃、不自行生成。
- 输出：该 agent 的每一行原样写出。
- 同一会话中，某个非空 `spawn_id` 第二次出现 `entry`：该 `entry` 行（及其后该次出现的在场行）`warning = duplicate_spawn_id`。用会话级集合判断，不看配置表是否事先重复。

配置表由你提供；本设计不往 `cfg.csv` 写入生成的 id。

## 10. 与现有代码的衔接点

| 现有位置 | 改动 |
|---|---|
| `Driver.Start` | 创建 `SessionLogger` 文件（可仍走 `XLogger.CreateCSV` 改写） |
| `BaseConfig` / `AgentConfig` | 读 `spawn_id` |
| `MainView` 1Hz 计时 | 停止在开局后无条件调用 `CombatMgr.Record()`；秒计数改由 `CombatMgr.UpdateGame` 驱动 |
| `CombatMgr.CreateItem` | `entry` 入队 |
| `CombatMgr.LoadGame` | 开局：缓存 level_start 时间戳，重置秒 |
| `CombatMgr.OnGameOver` / `RealExitGame` / 进下一关 | 区分“死亡续关”与“真正离开关卡”，后者 `level_end` |
| `Player.Catch` | `hook_fire` |
| `Player` hook miss 分支 | `hook_miss` |
| `Player.OnSuccessCatch` | 抓住结算移到此处；`hook_hit`；标记离场 |
| `Player.OnHit` | 未抓住 → `collision` 并标记离场；已抓住 → 不再结算 |
| `Player.BeHurt` | `shot`（带该 Threat 的 spawn_id） |
| `ShieldComp` miss | `block_miss` |
| `CombatMgr.OnProtectSuccess` | `block`；标记离场 |
| `CombatMgr.Record` / `RecordUtils` 拼接 | 不再写正式 CSV |

## 11. 错误处理

- 日志文件未打开：入队与 flush 静默跳过（与现有 `XLogger` 一致）。
- `spawn_id` 列缺失：当空字符串，启动警告一次。
- 重复 `spawn_id` 进场：行内 `warning`，不中断游戏。
- 关卡结束时若本秒已有部分事件未 flush：先 flush 当前秒（含这些事件），再写 `level_end`（可与最后事件同一 `second`）。

## 12. 验证

实现后用一局短打对照，至少覆盖：

- 第 1 秒有 `level_start` 且带两个时间戳；过关或退出有 `level_end`。
- 在场 agent 每秒都有行；空秒有且仅有一行无 agent 的记录。
- hook：`hook_fire` 无 spawn_id → 中间秒目标仍在场 → `hook_hit` 有 spawn_id 且为最后一行，delta 为抓住结算。
- hook 落空：`hook_fire` 与后续某秒 `hook_miss` 都无 spawn_id。
- block 成功：只有一行 `block`，带 spawn_id，delta 为 0，之后无该 agent 行。
- Threat：`shot` 每秒可重复，同一 `spawn_id`；最终 `collision` 或 `block` / `hook_hit`。
- collision：`avatar_gold_delta = -30`，`avatar_hp_delta = 0`。
- 同一秒多行时 `avatar_hp` / `avatar_gold` 随行变化，单元格无逗号拼接。
- `second` 连续、每关从 1 起；暂停时不跳秒。
- 文件头为 `schema_version=v2`。

不要求自动化单测（现有工程无测试框架）；用实机对局 + 打开 CSV 人工核对。
