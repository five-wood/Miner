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
