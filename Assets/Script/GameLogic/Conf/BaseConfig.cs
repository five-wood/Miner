using UnityEngine;
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
                levelConfigs[level].Add(agentConfig);
            }
            //按时间排序,从早到晚
            foreach(var item in levelConfigs)
            {
                item.Value.Sort((a, b) => a.bornTime.CompareTo(b.bornTime));
            }
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