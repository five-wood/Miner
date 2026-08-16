using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace Miner.GameLogic
{
    public class BaseConfig
    {

        public float moveSpeed = 5f;

        public static Dictionary<int, List<AgentConfig>> levelConfigs = new Dictionary<int, List<AgentConfig>>();

        public virtual void InitConfig(){}

        private static bool hadInit = false;
        private static bool warnedMissingSpawnId = false;
        private static readonly Dictionary<string, List<string>> spawnIdOccurrences = new Dictionary<string, List<string>>();

        public static int maxLevel = -1;

        //从csv文件中读取配置
        public static void InitAllLevel(string path)
        {
            if(hadInit)
            {
                return;
            }
            hadInit = true;
            List<Dictionary<string, string>> data = CSVReader.ReadCSV(path);
            Debug.Log("data.Count = "+data.Count);
            foreach(var item in data)
            {
                int level = int.Parse(item["Run"]);
                if(maxLevel<level)
                {
                    maxLevel = level;
                }
                if(!levelConfigs.ContainsKey(level))
                {
                    levelConfigs[level] = new List<AgentConfig>();
                }
                AgentConfig agentConfig = new AgentConfig();
                agentConfig.level = int.Parse(item["Run"]);
                agentConfig.wave = int.Parse(item["Wave"]);
                agentConfig.bornTime = float.Parse(item["Run Time"]);
                agentConfig.agentName = item["Agent"];
                agentConfig.posType = GetPosEnum(item["Position"]);
                agentConfig.speed = float.Parse(item["Speed"]);
                agentConfig.atkInterval = float.Parse(item["Interval to Atk"]);
                string spawnId = ReadSpawnId(item);
                if (spawnId != null)
                {
                    agentConfig.spawnId = spawnId;
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
                RecordSpawnIdOccurrence(agentConfig);
                levelConfigs[level].Add(agentConfig);
            }
            WarnDuplicateSpawnIds();
            //按时间排序,从早到晚
            foreach(var item in levelConfigs)
            {
                item.Value.Sort((a, b) => a.bornTime.CompareTo(b.bornTime));
            }
        }

        private static void RecordSpawnIdOccurrence(AgentConfig agentConfig)
        {
            if (string.IsNullOrEmpty(agentConfig.spawnId))
            {
                return;
            }
            string loc = string.Format("Run {0} Wave {1} {2}", agentConfig.level, agentConfig.wave, agentConfig.agentName);
            if (!spawnIdOccurrences.ContainsKey(agentConfig.spawnId))
            {
                spawnIdOccurrences[agentConfig.spawnId] = new List<string>();
            }
            spawnIdOccurrences[agentConfig.spawnId].Add(loc);
        }

        private static void WarnDuplicateSpawnIds()
        {
            List<string> lines = new List<string>();
            foreach (var kv in spawnIdOccurrences)
            {
                if (kv.Value.Count > 1)
                {
                    lines.Add(kv.Key + " ×" + kv.Value.Count + "：\n  " + string.Join("\n  ", kv.Value.ToArray()));
                }
            }
            if (lines.Count == 0)
            {
                return;
            }
            string message = "cfg.csv 中存在重复的 spawn_id：\n\n" + string.Join("\n\n", lines.ToArray());
            Debug.LogWarning(message);
            CSVReader.ShowNativePopup(message, "配置警告");
        }

        private static string ReadSpawnId(Dictionary<string, string> item)
        {
            foreach (var key in item.Keys)
            {
                if (string.Equals(key, "spawn_id", StringComparison.OrdinalIgnoreCase))
                {
                    return item[key] == null ? "" : item[key].Trim();
                }
            }
            return null;
        }

        private static PositionEnum GetPosEnum(string posType)
        {
            switch(posType)
            {
                case "Top-Left":
                    return PositionEnum.TopLeft;
                case "Top-Right":
                    return PositionEnum.TopRight;
                case "Bottom-Left":
                    return PositionEnum.BottomLeft;
                case "Bottom-Right":
                    return PositionEnum.BottomRight;
                default:
                    Debug.LogError("GetPosEnum error: " + posType);
                    return PositionEnum.TopLeft;
            }
        }

        public static List<AgentConfig> GetLevelConfig(int level)
        {
            if(!levelConfigs.ContainsKey(level))
            {
                Debug.LogError("GetLevelConfig error: " + level);
                return null;
            }
            return levelConfigs[level];
        }


    }
}