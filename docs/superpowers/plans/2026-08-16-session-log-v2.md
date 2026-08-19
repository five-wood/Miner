# Session Log v2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the per-second wide-table CSV with a long-table session log (schema v2) that writes one row per in-field agent per second, structured events, and level timestamps.

**Architecture:** A `SessionLogger` queues structured events during play and flushes a long table every game second (accumulator keeps the remainder). Combat code only enqueues at existing hook/block/hit/spawn call sites. `spawn_id` is read from `cfg.csv` and passed through unchanged.

**Tech Stack:** Unity C# (`Assembly-CSharp`), `System.IO.StreamWriter`, existing `cfg.csv` + `CSVReader`. No new packages. No test framework in this repo — verify by Unity compile and inspecting `Assets/log_*.csv`.

**Spec:** `docs/superpowers/specs/2026-08-16-session-log-v2-design.md`

## Global Constraints

- File header first line is exactly `# schema_version=v2`.
- Column order is exactly: `second,participant_id,level,wave,spawn_id,agent_type,n_reward,n_threat,n_coactive_fat,n_coactive_tall,event_type,avatar_hp_delta,avatar_gold_delta,avatar_hp,avatar_gold,unix_timestamp,datetime,in_field,distance,warning`
- `participant_id` is always empty this round.
- Never generate or rewrite `spawn_id`; read `cfg.csv` column `spawn_id` or leave empty.
- One value per cell; use `CultureInfo.InvariantCulture` so decimals never become commas.
- `second` starts at 1 per level; `elapsed += dt` then `elapsed -= 1` (do not zero the remainder).
- Collision stays gold −30, hp 0. Hook settlement moves to `OnSuccessCatch`. Successful block is one `block` row, no `block_fire`.
- Death + `ContinueGame` does not write `level_end` and does not reset `second`.
- Do not keep the old wide-table columns in the official CSV.

## File map

| File | Role |
|---|---|
| Create `Assets/Script/Utils/SessionLogger.cs` | File, clock, queue, flush, row format |
| Modify `Assets/Script/GameLogic/Conf/AgentConfig.cs` | `spawnId` field |
| Modify `Assets/Script/GameLogic/Conf/BaseConfig.cs` | Read `spawn_id` |
| Modify `Assets/Script/Utils/EntityUtils.cs` | `GetLogAgentType` |
| Modify `Assets/Script/GameLogic/Entity/BaseEntity.cs` | `logEnteredAt` |
| Modify `Assets/Script/GameLogic/CombatMgr.cs` | Tick, start/end level, `entry`/`block`, snapshots; delete wide `Record()` |
| Modify `Assets/Script/GameLogic/Entity/Player.cs` | `hook_fire` / `hook_miss` / `hook_hit` / `collision` / `shot` |
| Modify `Assets/Script/GameLogic/Component/ShieldComp.cs` | `block_miss` |
| Modify `Assets/Script/UI/MainView.cs` | Stop 1Hz `Record()` |
| Modify `Assets/Script/Driver.cs` | Create/stop session file |
| Modify `Assets/Script/Utils/XLogger.cs` | Stop writing the old CSV header |

---

### Task 1: SessionLogger

**Files:**
- Create: `Assets/Script/Utils/SessionLogger.cs`

**Interfaces:**
- Consumes: nothing from later tasks
- Produces: `SessionLogEvent`, `AgentSnapshot`, `SessionLogger.Instance` with `CreateFile(string)`, `StartLevel(int)`, `Enqueue(SessionLogEvent)`, `Tick(float,int,int,List<AgentSnapshot>)`, `EndLevel(int,int,List<AgentSnapshot>)`, `Stop()`, `IsExited(int)`, `MarkExited(int)`, `RegisterSpawnId(string)`, `IsDuplicateSpawn(string)`

- [ ] **Step 1: Create `Assets/Script/Utils/SessionLogger.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Miner.GameLogic
{
    public class SessionLogEvent
    {
        public string EventType;
        public int EntityId;
        public string SpawnId;
        public string AgentType;
        public int Wave;
        public int HpDelta;
        public int GoldDelta;
        public float Distance = float.NaN;
        public bool InField;
        public bool DuplicateSpawnWarning;
    }

    public class AgentSnapshot
    {
        public int EntityId;
        public string SpawnId;
        public string AgentType;
        public int Wave;
        public float Distance;
        public float EnteredAt;
        public bool DuplicateSpawnWarning;
    }

    public class SessionLogger
    {
        public const string SchemaVersion = "v2";
        public const string Header = "second,participant_id,level,wave,spawn_id,agent_type,n_reward,n_threat,n_coactive_fat,n_coactive_tall,event_type,avatar_hp_delta,avatar_gold_delta,avatar_hp,avatar_gold,unix_timestamp,datetime,in_field,distance,warning";

        private static SessionLogger _instance;
        public static SessionLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SessionLogger();
                }
                return _instance;
            }
        }

        private StreamWriter _writer;
        private readonly List<SessionLogEvent> _queue = new List<SessionLogEvent>();
        private readonly HashSet<string> _seenSpawnIds = new HashSet<string>();
        private readonly HashSet<string> _duplicateSpawnIds = new HashSet<string>();
        private readonly HashSet<int> _exitedIds = new HashSet<int>();

        private bool _levelActive;
        private bool _pendingLevelStart;
        private int _level;
        private int _second;
        private float _secondAccum;
        private long _levelStartUnixMs;
        private string _levelStartDatetime;

        public bool IsExited(int entityId)
        {
            return _exitedIds.Contains(entityId);
        }

        public void MarkExited(int entityId)
        {
            if (entityId != 0)
            {
                _exitedIds.Add(entityId);
            }
        }

        public bool RegisterSpawnId(string spawnId)
        {
            if (string.IsNullOrEmpty(spawnId))
            {
                return false;
            }
            if (_seenSpawnIds.Contains(spawnId))
            {
                _duplicateSpawnIds.Add(spawnId);
                return true;
            }
            _seenSpawnIds.Add(spawnId);
            return false;
        }

        public bool IsDuplicateSpawn(string spawnId)
        {
            return !string.IsNullOrEmpty(spawnId) && _duplicateSpawnIds.Contains(spawnId);
        }

        public void CreateFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            _writer = new StreamWriter(path, false, new UTF8Encoding(false));
            _writer.WriteLine("# schema_version=" + SchemaVersion);
            _writer.WriteLine(Header);
            _writer.Flush();
        }

        public void StartLevel(int level)
        {
            _level = level;
            _second = 0;
            _secondAccum = 0f;
            _levelActive = true;
            _pendingLevelStart = true;
            _queue.Clear();
            _exitedIds.Clear();
            DateTimeOffset now = DateTimeOffset.Now;
            _levelStartUnixMs = now.ToUnixTimeMilliseconds();
            _levelStartDatetime = now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        public void Enqueue(SessionLogEvent evt)
        {
            if (_writer == null || !_levelActive || evt == null)
            {
                return;
            }
            _queue.Add(evt);
        }

        public void Tick(float deltaTime, int avatarHp, int avatarGold, List<AgentSnapshot> agents)
        {
            if (_writer == null || !_levelActive)
            {
                return;
            }
            _secondAccum += deltaTime;
            while (_secondAccum >= 1f)
            {
                _second++;
                FlushSecond(avatarHp, avatarGold, agents);
                _secondAccum -= 1f;
            }
        }

        public void EndLevel(int avatarHp, int avatarGold, List<AgentSnapshot> agents)
        {
            if (_writer == null || !_levelActive)
            {
                return;
            }
            if (_secondAccum > 0f || _queue.Count > 0 || _pendingLevelStart)
            {
                _second++;
                FlushSecond(avatarHp, avatarGold, agents);
                _secondAccum = 0f;
            }
            if (_second < 1)
            {
                _second = 1;
            }
            WriteLevelEnd(avatarHp, avatarGold, agents);
            _levelActive = false;
            _queue.Clear();
            _writer.Flush();
        }

        public void Stop()
        {
            if (_writer == null)
            {
                return;
            }
            _writer.Flush();
            _writer.Dispose();
            _writer = null;
            _levelActive = false;
        }

        private void FlushSecond(int avatarHp, int avatarGold, List<AgentSnapshot> agents)
        {
            if (agents == null)
            {
                agents = new List<AgentSnapshot>();
            }

            int sumHp = 0;
            int sumGold = 0;
            HashSet<int> eventIds = new HashSet<int>();
            for (int i = 0; i < _queue.Count; i++)
            {
                sumHp += _queue[i].HpDelta;
                sumGold += _queue[i].GoldDelta;
                if (_queue[i].EntityId != 0)
                {
                    eventIds.Add(_queue[i].EntityId);
                }
            }
            int hp = avatarHp - sumHp;
            int gold = avatarGold - sumGold;
            CountTypes(agents, _queue, out int nReward, out int nThreat, out int nFat, out int nTall);

            int rows = 0;
            if (_pendingLevelStart)
            {
                WriteRow(_second, _level, 0, "", "", nReward, nThreat, nFat, nTall, "level_start", 0, 0, hp, gold, _levelStartUnixMs, _levelStartDatetime, 0, float.NaN, "");
                rows++;
                _pendingLevelStart = false;
            }

            List<AgentSnapshot> presence = new List<AgentSnapshot>();
            for (int i = 0; i < agents.Count; i++)
            {
                if (!eventIds.Contains(agents[i].EntityId))
                {
                    presence.Add(agents[i]);
                }
            }
            presence.Sort((a, b) => a.EnteredAt.CompareTo(b.EnteredAt));
            for (int i = 0; i < presence.Count; i++)
            {
                AgentSnapshot a = presence[i];
                string warning = a.DuplicateSpawnWarning ? "duplicate_spawn_id" : "";
                WriteRow(_second, _level, a.Wave, a.SpawnId, a.AgentType, nReward, nThreat, nFat, nTall, "", 0, 0, hp, gold, 0, "", 1, a.Distance, warning);
                rows++;
            }

            for (int i = 0; i < _queue.Count; i++)
            {
                SessionLogEvent e = _queue[i];
                hp += e.HpDelta;
                gold += e.GoldDelta;
                int wave = e.InField ? e.Wave : 0;
                string spawn = e.InField ? (e.SpawnId ?? "") : "";
                string agentType = e.InField ? (e.AgentType ?? "") : "";
                float dist = e.InField ? e.Distance : float.NaN;
                string warning = e.DuplicateSpawnWarning ? "duplicate_spawn_id" : "";
                WriteRow(_second, _level, wave, spawn, agentType, nReward, nThreat, nFat, nTall, e.EventType, e.HpDelta, e.GoldDelta, hp, gold, 0, "", e.InField ? 1 : 0, dist, warning);
                rows++;
            }

            if (rows == 0)
            {
                WriteRow(_second, _level, 0, "", "", nReward, nThreat, nFat, nTall, "", 0, 0, hp, gold, 0, "", 0, float.NaN, "");
            }
            _queue.Clear();
        }

        private void WriteLevelEnd(int avatarHp, int avatarGold, List<AgentSnapshot> agents)
        {
            CountTypes(agents, null, out int nReward, out int nThreat, out int nFat, out int nTall);
            DateTimeOffset now = DateTimeOffset.Now;
            WriteRow(_second, _level, 0, "", "", nReward, nThreat, nFat, nTall, "level_end", 0, 0, avatarHp, avatarGold, now.ToUnixTimeMilliseconds(), now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 0, float.NaN, "");
        }

        private static void CountTypes(List<AgentSnapshot> agents, List<SessionLogEvent> events, out int nReward, out int nThreat, out int nFat, out int nTall)
        {
            nReward = 0;
            nThreat = 0;
            nFat = 0;
            nTall = 0;
            HashSet<int> seen = new HashSet<int>();
            if (agents != null)
            {
                for (int i = 0; i < agents.Count; i++)
                {
                    if (seen.Add(agents[i].EntityId))
                    {
                        AddType(agents[i].AgentType, ref nReward, ref nThreat, ref nFat, ref nTall);
                    }
                }
            }
            if (events != null)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    SessionLogEvent e = events[i];
                    if (!e.InField || e.EntityId == 0)
                    {
                        continue;
                    }
                    if (seen.Add(e.EntityId))
                    {
                        AddType(e.AgentType, ref nReward, ref nThreat, ref nFat, ref nTall);
                    }
                }
            }
        }

        private static void AddType(string agentType, ref int nReward, ref int nThreat, ref int nFat, ref int nTall)
        {
            if (agentType == "Reward") nReward++;
            else if (agentType == "Threat") nThreat++;
            else if (agentType == "Coactive_Fat") nFat++;
            else if (agentType == "Coactive_Tall") nTall++;
        }

        private void WriteRow(int second, int level, int wave, string spawnId, string agentType,
            int nReward, int nThreat, int nFat, int nTall, string eventType, int hpDelta, int goldDelta,
            int hp, int gold, long unixMs, string datetime, int inField, float distance, string warning)
        {
            if (_writer == null)
            {
                return;
            }
            CultureInfo inv = CultureInfo.InvariantCulture;
            string waveCell = wave > 0 ? wave.ToString(inv) : "";
            string unixCell = unixMs > 0 ? unixMs.ToString(inv) : "";
            string distCell = float.IsNaN(distance) ? "" : distance.ToString("0.###", inv);
            string[] cells =
            {
                second.ToString(inv),
                "",
                level.ToString(inv),
                waveCell,
                spawnId ?? "",
                agentType ?? "",
                nReward.ToString(inv),
                nThreat.ToString(inv),
                nFat.ToString(inv),
                nTall.ToString(inv),
                eventType ?? "",
                hpDelta.ToString(inv),
                goldDelta.ToString(inv),
                hp.ToString(inv),
                gold.ToString(inv),
                unixCell,
                datetime ?? "",
                inField.ToString(inv),
                distCell,
                warning ?? ""
            };
            _writer.WriteLine(string.Join(",", cells));
        }
    }
}
```

- [ ] **Step 2: Confirm Unity compiles `SessionLogger.cs`**

Switch back to the Unity Editor and wait for `Assembly-CSharp` to rebuild. Expected: no error on `SessionLogger`. The class is unused until Task 3.

- [ ] **Step 3: Commit**

```bash
git add Assets/Script/Utils/SessionLogger.cs
git commit -m "feat: add SessionLogger for long-table v2 flush"
```

---

### Task 2: spawn_id and agent type

**Files:**
- Modify: `Assets/Script/GameLogic/Conf/AgentConfig.cs`
- Modify: `Assets/Script/GameLogic/Conf/BaseConfig.cs`
- Modify: `Assets/Script/Utils/EntityUtils.cs`
- Modify: `Assets/Script/GameLogic/Entity/BaseEntity.cs`

**Interfaces:**
- Consumes: `SessionLogger` not required yet
- Produces: `AgentConfig.spawnId`, `EntityUtils.GetLogAgentType(BaseEntity)`, `BaseEntity.logEnteredAt`

- [ ] **Step 1: Add `spawnId` to `AgentConfig`**

In `Assets/Script/GameLogic/Conf/AgentConfig.cs`, add this field after `atkInterval`:

```csharp
        public string spawnId;
```

- [ ] **Step 2: Read `spawn_id` in `BaseConfig.InitAllLevel`**

In `Assets/Script/GameLogic/Conf/BaseConfig.cs`, add a static flag near `hadInit`:

```csharp
        private static bool warnedMissingSpawnId = false;
```

Inside the `foreach (var item in data)` loop, after `agentConfig.atkInterval = ...` and before `levelConfigs[level].Add(agentConfig)`:

```csharp
                if (item.ContainsKey("spawn_id"))
                {
                    agentConfig.spawnId = item["spawn_id"] == null ? "" : item["spawn_id"].Trim();
                }
                else
                {
                    agentConfig.spawnId = "";
                    if (!warnedMissingSpawnId)
                    {
                        warnedMissingSpawnId = true;
                        Debug.LogWarning("cfg.csv missing spawn_id column; logs will leave spawn_id empty.");
                    }
                }
```

Do not invent an id when the column is missing or blank.

- [ ] **Step 3: Add `GetLogAgentType` to `EntityUtils`**

Append this method inside `EntityUtils` in `Assets/Script/Utils/EntityUtils.cs`:

```csharp
        public static string GetLogAgentType(BaseEntity entity)
        {
            if (entity is FatMushroom)
            {
                return "Coactive_Fat";
            }
            if (entity is TallMushroom)
            {
                return "Coactive_Tall";
            }
            if (entity is Threat)
            {
                return "Threat";
            }
            if (entity is Reward)
            {
                return "Reward";
            }
            return "";
        }
```

Check Fat/Tall before the `Coactive`/`Reward` bases. `Lucky grass` is `LuckyGrass : Reward`.

- [ ] **Step 4: Add `logEnteredAt` on `BaseEntity`**

In `Assets/Script/GameLogic/Entity/BaseEntity.cs`, after `public bool isDestroy = false;`:

```csharp
        public float logEnteredAt = 0f;
```

- [ ] **Step 5: Compile in Unity**

Expected: no errors. Console may show the missing-`spawn_id` warning on Play until the new `cfg.csv` is provided.

- [ ] **Step 6: Commit**

```bash
git add Assets/Script/GameLogic/Conf/AgentConfig.cs Assets/Script/GameLogic/Conf/BaseConfig.cs Assets/Script/Utils/EntityUtils.cs Assets/Script/GameLogic/Entity/BaseEntity.cs
git commit -m "feat: read spawn_id and map log agent types"
```

---

### Task 3: Clock, file, level start/end

**Files:**
- Modify: `Assets/Script/Driver.cs`
- Modify: `Assets/Script/Utils/XLogger.cs`
- Modify: `Assets/Script/UI/MainView.cs`
- Modify: `Assets/Script/GameLogic/CombatMgr.cs`

**Interfaces:**
- Consumes: `SessionLogger.Instance.CreateFile/StartLevel/Tick/EndLevel/Stop`, `EntityUtils.GetLogAgentType`, `AgentConfig.spawnId`, `BaseEntity.logEnteredAt`
- Produces: `CombatMgr.CollectAgentSnapshots()`, official CSV created at startup, old 1Hz wide `Record()` no longer called

- [ ] **Step 1: Point `Driver` at `SessionLogger`**

In `Assets/Script/Driver.cs` `Start()`, replace `XLogger.CreateCSV(logPath);` with:

```csharp
        SessionLogger.Instance.CreateFile(logPath);
```

In `OnQuitting()`, before `XLogger.Stop();`:

```csharp
        CombatMgr combat = CombatMgr.Instance();
        if (combat.player != null)
        {
            SessionLogger.Instance.EndLevel((int)combat.player.hp, combat.player.point, combat.CollectAgentSnapshots());
        }
        SessionLogger.Instance.Stop();
```

Keep `XLogger.Stop()` or delete that call if `XLogger` no longer owns a writer (Step 2).

- [ ] **Step 2: Stop writing the old CSV header**

In `Assets/Script/Utils/XLogger.cs`, change `CreateCSV` to a no-op (keep the method so nothing else breaks):

```csharp
        public static void CreateCSV(string path)
        {
        }
```

Change `Record` so it does not increment a global second or write a line:

```csharp
        public static void Record(string msg)
        {
        }
```

Leave `Stop()` disposing `writer` only when `writer != null` (already the case). After Driver stops calling `CreateCSV`, `writer` stays null.

- [ ] **Step 3: Remove MainView 1Hz `Record()`**

In `Assets/Script/UI/MainView.cs`, delete `startRecord`, `recordDuration`, the assignments in `OnStartGame`, and this block in `Update()`:

```csharp
            if (startRecord)
            {
                recordDuration += Time.deltaTime;
                if(recordDuration>1)
                {
                    CombatMgr.Instance().Record();
                    recordDuration = 0;
                }
            }
```

`OnStartGame` should only call `StartGameByLv(1);`.

- [ ] **Step 4: Add snapshot + clock + level hooks on `CombatMgr`**

Add this method to `CombatMgr` (public, used by Driver and `EndLevel` call sites):

```csharp
        public List<AgentSnapshot> CollectAgentSnapshots()
        {
            List<AgentSnapshot> list = new List<AgentSnapshot>();
            if (player == null || entityDict == null)
            {
                return list;
            }
            foreach (var kv in entityDict)
            {
                BaseEntity entity = kv.Value;
                if (entity == null || entity == player || entity.isDestroy || entity.config == null)
                {
                    continue;
                }
                if (SessionLogger.Instance.IsExited(entity.Id))
                {
                    continue;
                }
                string spawnId = entity.config.spawnId ?? "";
                list.Add(new AgentSnapshot
                {
                    EntityId = entity.Id,
                    SpawnId = spawnId,
                    AgentType = EntityUtils.GetLogAgentType(entity),
                    Wave = entity.config.wave,
                    Distance = Vector3.Distance(entity.GetPosition(), player.GetPosition()),
                    EnteredAt = entity.logEnteredAt,
                    DuplicateSpawnWarning = SessionLogger.Instance.IsDuplicateSpawn(spawnId)
                });
            }
            return list;
        }
```

At the end of `LoadGame`, after `CreatePlayer();`:

```csharp
            SessionLogger.Instance.StartLevel(this.level);
```

At the end of `UpdateGame`, after the game-over check (so a frame that ends the level still ticked first — actually Tick must run **before** `OnGameOver` so the last second includes that frame's events; put Tick after entity updates and **before** the hp/clear check):

```csharp
            if (player != null && !player.isDestroy)
            {
                SessionLogger.Instance.Tick(deltaTime, (int)player.hp, player.point, CollectAgentSnapshots());
            }
```

Place this after delayed-destroy cleanup and before `if (player.hp<=0 || ...)`.

In `OnGameOver`, after `isGameOver = true` and `bool isWin = player.hp > 0;`, add:

```csharp
            if (isWin)
            {
                SessionLogger.Instance.EndLevel((int)player.hp, player.point, CollectAgentSnapshots());
            }
```

In `ExitGame()`, before `isGameOver = true`:

```csharp
            if (player != null && !player.isDestroy)
            {
                SessionLogger.Instance.EndLevel((int)player.hp, player.point, CollectAgentSnapshots());
            }
```

`EndLevel` is a no-op if the level already ended (win then quit). Death does not call `EndLevel`. `ContinueGame` must not call `StartLevel`.

- [ ] **Step 5: Delete the wide-table `Record()` and `NearestAgent`**

Remove `CombatMgr.Record()` (the whole method starting at `public void Record()`) and `NearestAgent(...)`. They are only used by the old CSV.

- [ ] **Step 6: Compile and smoke the file header**

Play, start level 1, quit to desktop. Open the newest `Assets/log_*.csv`. Expected first two lines:

```
# schema_version=v2
second,participant_id,level,wave,spawn_id,agent_type,n_reward,n_threat,n_coactive_fat,n_coactive_tall,event_type,avatar_hp_delta,avatar_gold_delta,avatar_hp,avatar_gold,unix_timestamp,datetime,in_field,distance,warning
```

Expected: a `level_start` row with unix + datetime, later a `level_end` row. `second` starts at 1. No old `Avatar_healthchange` header.

- [ ] **Step 7: Commit**

```bash
git add Assets/Script/Driver.cs Assets/Script/Utils/XLogger.cs Assets/Script/UI/MainView.cs Assets/Script/GameLogic/CombatMgr.cs
git commit -m "feat: drive v2 session log clock and level bounds"
```

---

### Task 4: Enqueue gameplay events

**Files:**
- Modify: `Assets/Script/GameLogic/CombatMgr.cs` (`CreateItem`, `OnProtectSuccess`)
- Modify: `Assets/Script/GameLogic/Entity/Player.cs`
- Modify: `Assets/Script/GameLogic/Component/ShieldComp.cs`

**Interfaces:**
- Consumes: `SessionLogger.Enqueue/MarkExited/RegisterSpawnId`, `EntityUtils.GetLogAgentType`
- Produces: event rows `entry`, `hook_fire`, `hook_miss`, `hook_hit`, `block`, `block_miss`, `collision`, `shot`

Helper to build an agent event (write this as a private method on `CombatMgr` and also use equivalent field fills in `Player`):

```csharp
        public static SessionLogEvent AgentEvent(string eventType, BaseEntity entity, int hpDelta, int goldDelta, bool inField, bool duplicateWarning)
        {
            float dist = float.NaN;
            Player p = CombatMgr.Instance().player;
            if (entity != null && p != null)
            {
                dist = Vector3.Distance(entity.GetPosition(), p.GetPosition());
            }
            return new SessionLogEvent
            {
                EventType = eventType,
                EntityId = entity != null ? entity.Id : 0,
                SpawnId = entity != null && entity.config != null ? (entity.config.spawnId ?? "") : "",
                AgentType = EntityUtils.GetLogAgentType(entity),
                Wave = entity != null && entity.config != null ? entity.config.wave : 0,
                HpDelta = hpDelta,
                GoldDelta = goldDelta,
                Distance = dist,
                InField = inField,
                DuplicateSpawnWarning = duplicateWarning
            };
        }
```

Put `AgentEvent` on `CombatMgr` as `public static` so `Player` / `ShieldComp` can call `CombatMgr.AgentEvent(...)`.

- [ ] **Step 1: `entry` in `CreateItem`**

After `entity.InitConfig(agentConfig);` and `entityDict.Add(...)`, before `return entity;`:

```csharp
            entity.logEnteredAt = this.gameTime;
            bool duplicate = SessionLogger.Instance.RegisterSpawnId(agentConfig.spawnId);
            SessionLogger.Instance.Enqueue(AgentEvent("entry", entity, 0, 0, true, duplicate));
```

- [ ] **Step 2: `block` in `OnProtectSuccess`**

After `entity.BeHitAway(pos);`:

```csharp
                SessionLogger.Instance.MarkExited(entity.Id);
                bool dup = SessionLogger.Instance.IsDuplicateSpawn(entity.config != null ? entity.config.spawnId : "");
                SessionLogger.Instance.Enqueue(AgentEvent("block", entity, 0, 0, true, dup));
```

- [ ] **Step 3: `block_miss` in `ShieldComp`**

In `OnUpdatePosition`, inside `if(duration>=totalTime)` after the existing miss logging:

```csharp
                    SessionLogger.Instance.Enqueue(new SessionLogEvent { EventType = "block_miss" });
```

- [ ] **Step 4: `hook_fire` and `hook_miss` in `Player`**

In `Catch`, after the existing `IsCasting` guard succeeds (with the other `RecordUtils` lines):

```csharp
            SessionLogger.Instance.Enqueue(new SessionLogEvent { EventType = "hook_fire" });
```

In the miss branch (`catchDuration>totalCatchTime`):

```csharp
                        SessionLogger.Instance.Enqueue(new SessionLogEvent { EventType = "hook_miss" });
```

- [ ] **Step 5: Move catch settlement to `OnSuccessCatch` and emit `hook_hit`**

Replace `OnSuccessCatch` so that after the hook attaches to the entity it applies hp/gold **once** and logs:

```csharp
        public void OnSuccessCatch(int entityId)
        {
            MoveableEntity entity = CombatMgr.Instance().GetEntityByID(entityId) as MoveableEntity;
            if(entity != null)
            {
                hookCollisionComp.Disable();
                catchEntityId = entityId;
                entity.go.transform.SetParent(hookHead.transform);
                entity.go.transform.localPosition = new Vector3(2,0,0);
                targetHookPos = entity.go.transform.position;
                Vector3 playerPos = go.transform.position;
                totalCatchTime = Vector3.Distance(targetHookPos, playerPos) / HOOK_MOVE_SPEED;
                catchDuration = 0;
                XLogger.Info(string.Format("catch [{0}]， pos={1}", entity.name, entity.GetPosition().ToString()));

                float hpChanged = entity.GenerateHp();
                int pointChanged = entity.GeneratePoint();
                CombatMgr.Instance().ChangeHp(hpChanged);
                hp = Mathf.Clamp(hp + hpChanged, 0, 100);
                point += pointChanged;
                CombatMgr.Instance().ChangePoint(pointChanged);

                SessionLogger.Instance.MarkExited(entity.Id);
                bool dup = SessionLogger.Instance.IsDuplicateSpawn(entity.config != null ? entity.config.spawnId : "");
                SessionLogger.Instance.Enqueue(CombatMgr.AgentEvent("hook_hit", entity, (int)hpChanged, pointChanged, true, dup));
            }
        }
```

- [ ] **Step 6: `OnHit` — collision only; no second catch settlement**

Replace `OnHit` with:

```csharp
        public void OnHit(MoveableEntity entity)
        {
            if(catchEntityId == entity.Id)
            {
                return;
            }
            int pointChanged = -30;
            point += pointChanged;
            CombatMgr.Instance().ChangePoint(pointChanged);
            SessionLogger.Instance.MarkExited(entity.Id);
            bool dup = SessionLogger.Instance.IsDuplicateSpawn(entity.config != null ? entity.config.spawnId : "");
            SessionLogger.Instance.Enqueue(CombatMgr.AgentEvent("collision", entity, 0, pointChanged, true, dup));
        }
```

Keep `CommonHitHandler` in the file if still referenced; after this change it may be unused — delete it if the compiler warns.

- [ ] **Step 7: `shot` in `BeHurt`**

At the end of `BeHurt`, after applying damage:

```csharp
            bool dup = SessionLogger.Instance.IsDuplicateSpawn(entity != null && entity.config != null ? entity.config.spawnId : "");
            SessionLogger.Instance.Enqueue(CombatMgr.AgentEvent("shot", entity, (int)damage, 0, true, dup));
```

`damage` is already negative (e.g. `-5`).

- [ ] **Step 8: Compile**

Expected: no CS errors. `RecordUtils` list appends may remain; they no longer write the official CSV.

- [ ] **Step 9: Commit**

```bash
git add Assets/Script/GameLogic/CombatMgr.cs Assets/Script/GameLogic/Entity/Player.cs Assets/Script/GameLogic/Component/ShieldComp.cs
git commit -m "feat: enqueue v2 hook block shot collision events"
```

---

### Task 5: Playtest against the spec checklist

**Files:**
- Test: newest `Assets/log_*.csv` after a short run
- No code unless a checklist item fails

**Interfaces:**
- Consumes: all of the above
- Produces: a CSV that matches spec section 12

- [ ] **Step 1: Play a short session covering the list below, then quit**

Open the newest `Assets/log_*.csv` (not an old wide-table file). Check:

1. Line 1 is `# schema_version=v2`. Line 2 is the v2 header. No cell contains two numbers joined by a comma.
2. First gameplay row is `event_type=level_start`, `second=1`, `unix_timestamp` and `datetime` filled; other rows have those two columns empty.
3. Quitting or winning writes `level_end` with both timestamps. Dying and waiting for continue does **not** write `level_end`; `second` keeps increasing after revive.
4. Each in-field agent has a row every second with `spawn_id` (empty until you add the column) and `agent_type` in `{Reward,Threat,Coactive_Fat,Coactive_Tall}`. `in_field=1` on those rows. `distance` is a number on those rows only.
5. Empty seconds: exactly one row, agent columns empty, `avatar_hp` / `avatar_gold` filled.
6. Left click: `hook_fire` with empty `spawn_id`. If it hits: later `hook_hit` with that agent's `spawn_id`, that is the agent's last row, deltas match catch rewards (e.g. Lucky grass +20/+20). If it misses: `hook_miss` with empty `spawn_id`.
7. Successful right-click block: one `block` row with `spawn_id`, both deltas 0, no later rows for that agent. Miss: `block_miss`, empty `spawn_id`.
8. Threat in range: `shot` rows with the same `spawn_id` and `avatar_hp_delta=-5`. Unblocked walk-in: `collision` with `avatar_gold_delta=-30`, `avatar_hp_delta=0`.
9. Same-second multi-row: `avatar_hp` / `avatar_gold` change row-by-row after each delta, not jumped to the final value on every row.
10. `second` is 1,2,3… with no ~97s skip. Pause (`Ctrl+P`) does not advance `second`.
11. `participant_id` is empty. `warning` is empty unless the same `spawn_id` entered twice this session.

- [ ] **Step 2: If any item fails, fix the matching task file and re-run that item**

Do not paper over a miss by changing the spec. The spec is the source of truth.

- [ ] **Step 3: Commit any playtest fixes**

```bash
git add -u
git commit -m "fix: session log v2 playtest gaps"
```

Skip this commit if there were no fixes.

---

## Self-review (plan vs spec)

| Spec section | Task |
|---|---|
| Long table, one value per cell, schema v2 header | 1, 3 |
| Agent identity on every in-field row | 2, 3 snapshots, 4 entry |
| `spawn_id` pass-through, no generation, duplicate warning | 2, 1 `RegisterSpawnId`, 4 entry |
| Event split (hook_fire/hit, block one row, player actions without id) | 4 |
| Row-by-row hp/gold | 1 `FlushSecond` |
| `second` + level timestamps | 1, 3 |
| Empty-second row | 1 |
| `in_field`, `n_*`, `distance` | 1, 3 |
| `participant_id` empty | 1 `WriteRow` |
| Collision −30 gold | 4 `OnHit` |
| Hook settle at contact | 4 `OnSuccessCatch` |
| Death continue no `level_end` | 3 `OnGameOver` |
| Clock remainder kept | 1 `Tick` |
| Manual CSV checklist | 5 |

No TBD placeholders. Names are consistent: `SessionLogger.Instance`, `AgentEvent`, `CollectAgentSnapshots`, `spawnId`, `GetLogAgentType`, `logEnteredAt`.
