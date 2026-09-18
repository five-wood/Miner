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
            File.WriteAllText(path, "Agent,HP,Gold,ShotHP\nCollision,-5,-8,0\n");
            BaseConfig.InitAgentStats(path);

            AgentStats collisionStats = BaseConfig.GetAgentStats("Collision");
            string tip = MainView.FormatLevelTip(collisionStats.hp, collisionStats.gold);

            Assert.IsTrue(tip.Contains("lose 8 gold points"));
            Assert.IsTrue(tip.Contains("lose 5 health points"));
            Assert.IsFalse(tip.Contains("lose 30 gold points"));
        }
        [Test]
        public void PickupHpKeepsConfiguredPositiveDisplayAtHealthCap()
        {
            float displayHp = Player.ResolvePickupHp(95f, 10f, out float resultingHp, out int effectiveHp);

            Assert.AreEqual(100f, resultingHp);
            Assert.AreEqual(5, effectiveHp);
            Assert.AreEqual(10f, displayHp);
        }

        [Test]
        public void LevelTipDescribesConfiguredCollisionHpAndGoldChanges()
        {
            string tip = MainView.FormatLevelTip(-5f, -8);

            Assert.IsTrue(tip.Contains("lose 8 gold points"));
            Assert.IsTrue(tip.Contains("lose 5 health points"));
        }
        [Test]
        public void FullHealthPickupDisplaysConfiguredHpWithoutExceedingCap()
        {
            float displayHp = Player.ResolvePickupHp(100f, 20f, out float resultingHp, out int effectiveHp);

            Assert.AreEqual(100f, resultingHp);
            Assert.AreEqual(0, effectiveHp);
            Assert.AreEqual(20f, displayHp);
        }

        [Test]
        public void PartialHealthPickupDisplaysConfiguredHp()
        {
            float displayHp = Player.ResolvePickupHp(90f, 20f, out float resultingHp, out int effectiveHp);

            Assert.AreEqual(100f, resultingHp);
            Assert.AreEqual(10, effectiveHp);
            Assert.AreEqual(20f, displayHp);
        }

        [Test]
        public void DamageDisplaysEffectiveLossWithoutDroppingBelowZero()
        {
            float displayHp = Player.ResolvePickupHp(5f, -10f, out float resultingHp, out int effectiveHp);

            Assert.AreEqual(0f, resultingHp);
            Assert.AreEqual(-5, effectiveHp);
            Assert.AreEqual(-5f, displayHp);
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
