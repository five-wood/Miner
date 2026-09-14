using System.IO;
using NUnit.Framework;
using Miner.GameLogic;
using Miner.UI;
using UnityEngine;

namespace Miner.Tests
{
    public class AgentStatsTests
    {
        [Test]
        public void LoadsFormalAgentStatsByName()
        {
            BaseConfig.InitAgentStats(Path.Combine(Application.dataPath, "agent_stats.csv"));

            AssertStats("Lucky grass", 20f, 20, 0f);
            AssertStats("Fat Mushroom", 20f, -20, 0f);
            AssertStats("Tall Mushroom", -20f, 20, 0f);
            AssertStats("Toxic Vine", -15f, -20, -5f);
            AssertStats("Collision", 0f, -30, 0f);
        }

        [Test]
        public void ReadsHpGoldShotFromCsvNotHardcoded()
        {
            string path = Path.Combine(Path.GetTempPath(), "agent_stats_not_hardcoded.csv");
            File.WriteAllText(path, "Agent,HP,Gold,ShotHP\nLucky grass,7,9,-3\nCollision,1,-8,0\n");

            BaseConfig.InitAgentStats(path);

            AssertStats("Lucky grass", 7f, 9, -3f);
            AssertStats("Collision", 1f, -8, 0f);
        }

        [Test]
        public void LevelTipUsesCollisionGoldLossFromConfig()
        {
            string path = Path.Combine(Path.GetTempPath(), "agent_stats_level_tip.csv");
            File.WriteAllText(path, "Agent,HP,Gold,ShotHP\nCollision,0,-8,0\n");
            BaseConfig.InitAgentStats(path);

            string tip = MainView.FormatLevelTip(Mathf.Abs(BaseConfig.GetAgentStats("Collision").gold));

            Assert.IsTrue(tip.Contains("lose 8 gold points"));
            Assert.IsFalse(tip.Contains("lose 30 gold points"));
        }

        private static void AssertStats(string agent, float hp, int gold, float shotHp)
        {
            AgentStats stats = BaseConfig.GetAgentStats(agent);
            Assert.AreEqual(hp, stats.hp);
            Assert.AreEqual(gold, stats.gold);
            Assert.AreEqual(shotHp, stats.shotHp);
        }
    }
}
