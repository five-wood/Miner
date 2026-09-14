# Agent 配置说明

两张表，启动时由 `Driver` 读取：

| 文件 | 作用 |
|---|---|
| `Assets/cfg.csv` | 每一行 = 一次出生（关卡时间轴） |
| `Assets/agent_stats.csv` | 按 **Agent 名字** 配 HP / Gold / 射击伤害 |

改完 CSV 后重新进游戏（或重启）才生效。Excel 打开时程序仍可读（`FileShare.ReadWrite`）。

`Agent` 名字必须两边一致。`CreateItem` 只认这四个字面量：

- `Lucky grass`
- `Fat Mushroom`
- `Tall Mushroom`
- `Toxic Vine`

写错会 `CreateItem error`，结算也会弹「缺少 Agent」。

---

## `cfg.csv`（出生表）

表头：

```
Run,Wave,Run Time,Total Time,Agent,Agent Type,Position,Speed,Interval to Atk,Interval to Next,Phase,Group ID,has_threat_agent,coactive_present,reward_present,spawn_id
```

加载后按 **Total Time 升序** 排队。`gameTime >= Total Time` 时生成。同一秒多行会同一帧一起出。

### 程序会读的列

| 列 | 代码字段 | 作用 |
|---|---|---|
| `Run` | `level` | 关卡号。`maxLevel` 取最大值。 |
| `Wave` | `wave` | 日志 / 死亡信息用。不控制出生。 |
| `Run Time` | `bornTime` | **会读入，出生不用。** 出生只看 `Total Time`。两列多数行相同；第 6 关第 3 波有差值（例如 Run Time=81, Total Time=80）。 |
| `Total Time` | `totalTime` | **出生时刻（秒）**。关卡时钟从 0 起。 |
| `Agent` | `agentName` | 决定实体类 + 查 `agent_stats.csv`。 |
| `Position` | `posType` | 出生角：`Top-Left` / `Top-Right` / `Bottom-Left` / `Bottom-Right`。其它值报错并回退 Top-Left。出生后朝玩家飞。 |
| `Speed` | `speed` | 飞向玩家的速度。 |
| `Interval to Atk` | `atkInterval` | 仅 **Toxic Vine** 射击间隔（秒）。其它类型读了不用。 |
| `spawn_id` | `spawnId` | 会话日志主键。空则日志空；重复会弹窗警告。不要改已有 ID 的写法，只保证不重复。 |

### 程序不读的列（给人看 / 实验标注）

改这些 **不影响游戏**：

- `Agent Type`（Reward / Coactive / Threat）— 类型由 `Agent` 名字对应的 C# 类决定
- `Interval to Next`
- `Phase`（Single-Agent / Multi-Agent）
- `Group ID`
- `has_threat_agent` / `coactive_present` / `reward_present`

同 `Group ID`、相同 `Total Time` 的多行 = 同时出场。游戏不校验 Group，只按时间出生。

---

## `agent_stats.csv`（数值表）

表头：

```
Agent,HP,Gold,ShotHP
```

按 `Agent` 精确匹配（含空格、大小写）。

| 列 | 何时用 |
|---|---|
| `HP` | 钩子抓住：玩家 HP `+= HP`，再夹到 `[0, 100]`。 |
| `Gold` | 钩子抓住：金币 `+= Gold`。 |
| `ShotHP` | 仅毒藤子弹命中：`BeHurt(ShotHP)`。其它类型填 `0`。 |

另有一行 **`Collision`**，不是出生单位：

| 用途 | 字段 |
|---|---|
| 漏网撞玩家 | 金币 `+= Gold`（现 `-30`），**不改 HP**，agent 立刻销毁 |
| 开场 `levelTipText` | `you'll lose {abs(Gold)} gold points` |

格挡（盾打飞）不走这张表：HP/Gold 都是 0。

当前默认：

| Agent | HP | Gold | ShotHP |
|---|---|---|---|
| Lucky grass | +20 | +20 | 0 |
| Fat Mushroom | +20 | −20 | 0 |
| Tall Mushroom | −20 | +20 | 0 |
| Toxic Vine | −15 | −20 | −5 |
| Collision | 0 | −30 | 0 |

---

## 结算对照

| 交互 | HP | Gold | 日志 `event_type` | agent 下场 |
|---|---|---|---|---|
| 钩子抓住 | 该 Agent 的 `HP` | 该 Agent 的 `Gold` | `hook_hit` | 被拉回 |
| 毒藤子弹 | `ShotHP` | 0 | `shot` | 还在场，可再射 |
| 盾格挡 | 0 | 0 | `block` | 击飞后延迟销毁 |
| 漏网碰撞 | 0 | `Collision.Gold` | `collision` | 立刻销毁 |

抓住 / 格挡 / 碰撞 三者互斥：已抓住或已离场的 agent 再撞玩家不会二次扣金。

---

## 改表注意

1. 逗号分隔，单元格里不要再写逗号。
2. `spawn_id` 全局唯一；重复只警告，仍会生成。
3. 新 Agent 名字要同时改：`cfg.csv`、`agent_stats.csv`、`CombatMgr.CreateItem` 的 switch。
4. `Agent Type` 改成 Reward 不会让蘑菇变幸运草；类由 `Agent` 列决定。
5. 缺文件或缺 Agent 名：Windows 弹原生对话框。
