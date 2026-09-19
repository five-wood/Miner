# Death Recovery and Timeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep level time continuous through a five-second death wait, skip already scheduled entries without replaying waves, and emit complete `death_wait` log rows.

**Architecture:** Move death recovery ownership into `CombatMgr`. Add `totalTime` to each `AgentConfig`, use it for both normal scheduling and recovery, and keep `lastAgentIndex` monotonic. Extend `SessionLogger` with a death-wait tick that shares its existing second accumulator. `MainView` renders manager state without owning or completing the recovery countdown.

**Tech Stack:** Unity 2021.3.36f1, C#, existing `CombatMgr`, `MainView`, `BaseConfig`, `SessionLogger`, CSV config.

---

### Task 1: Add the configured total-time field

**Files:**
- Create: `Assets/Tests/Editor/DeathRecoveryTests.cs`
- Modify: `Assets/Script/GameLogic/Conf/AgentConfig.cs:7-17`
- Modify: `Assets/Script/GameLogic/Conf/BaseConfig.cs:44-74`

- [ ] **Step 1: Write the failing config test**

Add an NUnit editor test that calls `BaseConfig.InitAllLevel(Path.Combine(Application.dataPath, "cfg.csv"))`, finds Run 6 spawn `L6_W3_RWD_01`, and asserts `bornTime == 81f` while `totalTime == 80f`. The test must fail to compile before the production field exists.

- [ ] **Step 2: Run the test and verify it fails**

Run the Unity EditMode test `DeathRecoveryTests.LoadsTotalTimeSeparatelyFromRunTime`. Expected: compilation failure because `AgentConfig.totalTime` does not exist.

- [ ] **Step 3: Implement the minimal field and parser assignment**

Add a field beside `bornTime`:

```csharp
public float bornTime;
public float totalTime;
```

Parse both columns in `BaseConfig.InitAllLevel`:

```csharp
agentConfig.bornTime = float.Parse(item["Run Time"]);
agentConfig.totalTime = float.Parse(item["Total Time"]);
```

Sort each level list by `totalTime`; equal-time rows may remain in either relative order because Task 2 drains the complete equal-time group in one update.

- [ ] **Step 4: Run the check and verify it passes**

Inspect rows from all six Runs. Expected: `totalTime` matches the `Total Time` column, including the Run 6 rows where `Run Time` and `Total Time` differ; values are monotonic within each Run.

- [ ] **Step 5: Commit**

```bash
git add Assets/Script/GameLogic/Conf/AgentConfig.cs Assets/Script/GameLogic/Conf/BaseConfig.cs
git commit -m "fix: load configured total agent time"
```

---

### Task 2: Make normal scheduling drain all due entries

**Files:**
- Modify: `Assets/Script/GameLogic/CombatMgr.cs:17-21,83-137,160-177`

- [ ] **Step 1: Write the failing scheduling test**

Add `FindLastDueConfigIndex` tests to `Assets/Tests/Editor/DeathRecoveryTests.cs`. With config times `[22, 28, 28, 36]`, cursor `0`, and time `28`, assert the returned last due index is `2`; with cursor `2` and time `35`, assert it remains `2`.

- [ ] **Step 2: Run the test and verify it fails**

Run `DeathRecoveryTests.FindsEveryConfigDueAtTheCurrentTime`. Expected: compilation failure because `CombatMgr.FindLastDueConfigIndex` does not exist.

- [ ] **Step 3: Implement the minimal scheduling loop**

Add one allocation-free helper and use it in `CheckRound`:

```csharp
public static int FindLastDueConfigIndex(List<AgentConfig> configs, int lastIndex, float currentTime)
{
    int index = lastIndex;
    while (index < configs.Count - 1 && configs[index + 1].totalTime <= currentTime)
    {
        index++;
    }
    return index;
}
```

`CheckRound` computes `dueThrough`, creates every config from `lastAgentIndex + 1` through `dueThrough`, then assigns `lastAgentIndex = dueThrough`. It returns `lastAgentIndex < configs.Count - 1`. Update `wave` for every created config and retain the existing entry logging. Do not introduce a second cursor.

- [ ] **Step 4: Run the focused scenario and verify it passes**

Expected: all entries at the same `Total Time` appear together; `lastAgentIndex` advances exactly once per config row; no row is created twice.

- [ ] **Step 5: Commit**

```bash
git add Assets/Tests/Editor/DeathRecoveryTests.cs Assets/Script/GameLogic/CombatMgr.cs
git commit -m "fix: schedule all agents due at the same time"
```

---

### Task 3: Add explicit death-wait logging

**Files:**
- Modify: `Assets/Script/Utils/SessionLogger.cs:9-37,143-175,211-279`

- [ ] **Step 1: Write the failing logger test**

In `Assets/Tests/Editor/DeathRecoveryTests.cs`, create a temporary CSV, call `SessionLogger.StartLevel(1)`, flush the level-start second, then call `TickDeathWait(5f, 37)`. Stop the logger and assert there are exactly five subsequent rows whose seconds are contiguous and whose fields are: empty spawn/agent, `death_wait`, HP `0`, gold `37`, four zero counts, `in_field=0`, and empty distance/warning.

- [ ] **Step 2: Run the test and verify it fails**

Run `DeathRecoveryTests.WritesOneDeathWaitRowForEveryElapsedSecond`. Expected: compilation failure because `SessionLogger.TickDeathWait` does not exist.

- [ ] **Step 3: Implement the death-wait tick**

Add this method, reusing `_secondAccum` so normal and death-wait rows share one clock:

```csharp
public void TickDeathWait(float deltaTime, int avatarGold)
{
    if (_writer == null || !_levelActive)
    {
        return;
    }
    _secondAccum += deltaTime;
    while (_secondAccum >= 1f)
    {
        _second++;
        WriteRow(_second, _level, 0, "", "", 0, 0, 0, 0,
            "death_wait", 0, 0, 0, avatarGold, 0, "", 0, float.NaN, "");
        _secondAccum -= 1f;
    }
    _writer.Flush();
}
```

Do not route this through `Enqueue`, because `Enqueue` rejects events outside active play. `BeginDeathWait` must first call `FlushPendingEvents(0, deathGold, emptySnapshots)` so the fatal second is retained and the shared accumulator begins the wait at zero.

- [ ] **Step 4: Run the logger test and verify it passes**

Expected: the fatal/level-start row is followed by exactly five valid `death_wait` rows at consecutive seconds.

- [ ] **Step 5: Commit**

```bash
git add Assets/Script/Utils/SessionLogger.cs
git commit -m "fix: log each second of death recovery"
```

---

### Task 4: Implement monotonic death recovery in CombatMgr

**Files:**
- Modify: `Assets/Script/GameLogic/CombatMgr.cs:16-21,64-177,224-301`

- [ ] **Step 1: Write the failing integration scenario**

Create a deterministic scenario that dies mid-wave at `gameTime=106`, has a next config entry at `Total Time=107`, and advances five seconds. Assert:

- non-player entities are destroyed immediately;
- `lastAgentIndex` never decreases;
- `gameTime` advances during all five wait seconds;
- the entry at 107 is not replayed and is skipped if the wait passes it;
- the first entry after the wait is the first config entry with `Total Time > gameTime`;
- no configured spawn ID appears twice.

- [ ] **Step 2: Run the scenario and verify it fails**

Before the change, `OnGameOver` sets `isGameOver=true`, so `UpdateGame` returns; `ContinueGame` rewinds to the previous wave start and resets the timeline. The scenario must show a paused clock and duplicate spawn IDs.

- [ ] **Step 3: Add manager-owned recovery state**

Add fields near the existing `pause` field:

```csharp
private bool deathWaiting = false;
private float deathWaitRemaining = 0f;
private int deathGold = 0;
private const float DeathWaitDuration = 5f;
```

Keep `isGameOver` for terminal state/UI. At the start of `UpdateGame`, return only when terminal or externally paused; otherwise always advance `gameTime` and call the logger clock. Branch agent simulation and scheduling when `deathWaiting` is true.

- [ ] **Step 4: Start death recovery without rewinding**

Replace the death branch in `UpdateGame` with a transition method that:

1. preserves the death second’s queued event;
2. stores the death gold value;
3. destroys every non-player entity;
4. sets `deathWaiting=true` and `deathWaitRemaining=5f`;
5. does not modify `gameTime` or `lastAgentIndex`;
6. does not call `DiscardPendingSecond`.

Use a separate cleanup helper so the dictionary is not modified during `foreach`.

- [ ] **Step 5: Advance the waiting state**

During each `UpdateGame(deltaTime)` while waiting, execute only:

```csharp
gameTime += deltaTime;
deathWaitRemaining -= deltaTime;
SessionLogger.Instance.TickDeathWait(deltaTime, deathGold);
```

Do not call ordinary `SessionLogger.Tick` for the same elapsed time. `TickDeathWait` owns the shared logger accumulator and emits exactly one `death_wait` row per completed second.

When `deathWaitRemaining <= 0`, set HP to 100 and point/gold to zero, advance `lastAgentIndex` to `FindLastDueConfigIndex(configs, lastAgentIndex, gameTime)` so wait-window entries are permanently skipped, then set `deathWaiting=false`. Do not call `CheckRound` immediately: by definition all entries at or before the recovery time were skipped, so the next normal update waits for the first future `totalTime`.

- [ ] **Step 6: Handle no-future-entry cases**

Add a forward-looking helper that checks whether any config row after `lastAgentIndex` has `totalTime > gameTime`. If none exists when death starts, finish terminal failure immediately without starting the wait. If none exists at recovery completion, finish terminal failure after writing the wait rows.

Implement terminal failure in one manager-owned path that writes any pending fatal event, calls `SessionLogger.EndLevel`, sends the failure message, and then invokes `mainView.HandleTerminalFailure(level, player.point)`. The callback starts `level + 1` only when `level < BaseConfig.maxLevel`; otherwise it displays final-level failure. Do not call `RealExitGame` before `HandleTerminalFailure` because that method destroys the player needed by the callback; `HandleTerminalFailure` performs cleanup before starting the next level.

- [ ] **Step 7: Run the integration scenario and verify it passes**

Expected: no rewind, no duplicate spawn warning, continuous `gameTime`, five death-wait seconds when future entries existed, and no level duration beyond the configured timeline. Verify a death with no future entry follows immediate terminal failure.

- [ ] **Step 8: Commit**

```bash
git add Assets/Script/GameLogic/CombatMgr.cs
 git commit -m "fix: resume levels after death without replaying waves"
```

---

### Task 5: Remove UI-owned recovery timing and wire automatic next-level flow

**Files:**
- Modify: `Assets/Script/UI/MainView.cs:42-45,118-152,240-255`
- Modify: `Assets/Script/GameLogic/CombatMgr.cs` terminal failure callback area

- [ ] **Step 1: Write the failing UI state test**

Add pure state assertions to `DeathRecoveryTests`: after `BeginDeathWait`, `IsDeathWaiting` is true and `DeathWaitRemaining` begins at 5; after four elapsed seconds it remains waiting; after the fifth it returns to active presentation. Add terminal routing assertions: a failed non-final level requests level `level + 1`, while the final level requests no next level.

- [ ] **Step 2: Run the tests and verify they fail**

Run `DeathRecoveryTests.ExposesManagerOwnedDeathCountdown` and `DeathRecoveryTests.RoutesTerminalFailureByLevel`. Expected: the manager properties/helpers do not exist and `MainView.Update` still completes recovery itself.

- [ ] **Step 3: Make UI a projection of manager state**

Expose read-only manager properties `IsDeathWaiting` and `DeathWaitRemaining`. Replace `MainView`'s `loseTimer` mutation with display-only rendering:

```csharp
if (CombatMgr.Instance().IsDeathWaiting)
{
    loseTimerTxt.text = string.Format("{0} seconds", Mathf.CeilToInt(CombatMgr.Instance().DeathWaitRemaining));
}
```

Delete the UI call to `CombatMgr.ContinueGame()` and delete the obsolete rewind implementation. `CombatMgr` calls `mainView.ShowGameplayAfterDeathWait()` when recovery finishes; that method only toggles `gamingGo`/`afterGo` and resets UI animations.

- [ ] **Step 4: Wire terminal next-level behavior**

Add a single `MainView` method for terminal failure routing:

```csharp
public void HandleTerminalFailure(int failedLevel, int score)
{
    if (failedLevel < BaseConfig.maxLevel)
    {
        StartGameByLv(failedLevel + 1);
        return;
    }
    OnGameOver(false, false, score);
}
```

`CombatMgr` must call `SessionLogger.EndLevel` and send the failure message before this callback. `HandleTerminalFailure` destroys old entities before the next `LoadGame` creates a player; it must not call `RealExitGame` because `RealExitGame` also emits a guarded `level_end` and would destroy the player before the callback.

- [ ] **Step 5: Run the UI scenario and verify it passes**

Expected: non-final terminal failure starts the next level; final-level failure remains visible; no UI timer changes `gameTime`; the recovery countdown and log seconds agree.

- [ ] **Step 6: Commit**

```bash
git add Assets/Script/UI/MainView.cs Assets/Script/GameLogic/CombatMgr.cs
 git commit -m "fix: centralize recovery timing and advance levels after failure"
```

---

### Task 6: Verify configured speed end to end

**Files:**
- Test: `Assets/Tests/Editor/DeathRecoveryTests.cs`
- Inspect: `Assets/Script/GameLogic/Conf/BaseConfig.cs`
- Inspect: `Assets/Script/GameLogic/CombatMgr.cs`
- Inspect: `Assets/Script/GameLogic/Entity/MoveableEntity.cs`
- Inspect: `Assets/Script/GameLogic/Component/FollowComp.cs`

- [ ] **Step 1: Add the config speed assertion**

Extend `LoadsTotalTimeSeparatelyFromRunTime` to assert formal spawn `L1_W1_RWD_01` loads speed `5f` and spawn `L1_W2_RWD_01` loads speed `5.5f`.

- [ ] **Step 2: Run the config test**

Expected: PASS after Task 1. This proves CSV parsing preserves distinct speed values.

- [ ] **Step 3: Smoke the runtime handoff**

In Play Mode, start Run 1 and inspect the first Wave 1 and Wave 2 spawned entities. Expected: `entity.config.speed` and `entity.followComp.speed` are respectively `5` and `5.5`. The required runtime path is:

```csharp
agentConfig.speed = float.Parse(item["Speed"]);
entity.SetTargetPos(targetPos, agentConfig.speed);
followComp.SetSpeed(speed);
```

- [ ] **Step 4: Record the result without speculative production changes**

If both inspected values match, leave the speed production code unchanged. A mismatch is a new reproduced bug: add a focused failing test for the exact broken handoff before changing that handoff.

---

### Task 7: Run final smoke verification and inspect logs

**Files:**
- Verify: `Assets/Script/GameLogic/CombatMgr.cs`
- Verify: `Assets/Script/Utils/SessionLogger.cs`
- Verify: `Assets/cfg.csv`

- [ ] **Step 1: Run the Unity play-mode smoke scenario**

Exercise at least one death in the middle of a wave, one death during the last configured segment, one same-time multi-agent group, and one non-final-level failure.

- [ ] **Step 2: Validate the generated CSV mechanically**

For each level log, assert:

```text
- every non-empty spawn_id occurs at most once;
- no warning cell contains duplicate_spawn_id;
- death_wait rows have empty spawn_id and agent_type;
- death_wait rows have avatar_hp=0;
- death_wait rows preserve the death gold;
- death_wait rows have all four agent counts equal to 0;
- second values are contiguous within the level;
- first post-recovery agent row has avatar_hp=100 and avatar_gold=0;
- `gameTime` never decreases, and a death run does not last longer than the same level's no-death baseline because of recovery.
```

- [ ] **Step 3: Run Unity EditMode tests and compile check**

With the Unity editor closed, run `"E:/Unity 2021.3.36f1/Editor/Unity.exe" -batchmode -projectPath "E:/Miner" -runTests -testPlatform EditMode -testResults "E:/Miner/Temp/death-recovery-tests.xml" -quit`. Expected: all `DeathRecoveryTests` pass and Unity exits with no C# compile errors.

- [ ] **Step 4: Review the complete diff for scope**

Confirm that only the approved death recovery, timeline, logging, UI transition, and speed-verification changes remain. Do not alter unrelated user changes in `Assets/cfg.csv` or `Assets/cfg11.csv`.

- [ ] **Step 5: Commit verification-related production changes if needed**

```bash
git add Assets/Script docs/superpowers
 git commit -m "test: verify death recovery timeline and logs"
```
