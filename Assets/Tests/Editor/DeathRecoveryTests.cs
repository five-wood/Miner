using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Miner.GameLogic;

namespace Miner.Tests
{
    public class DeathRecoveryTests
    {
        [Test]
        public void LoadsTotalTimeSeparatelyFromRunTime()
        {
            AgentConfig waveOne = new AgentConfig
            {
                bornTime = 1f,
                totalTime = 1f,
                speed = 5f
            };
            AgentConfig waveTwo = new AgentConfig
            {
                bornTime = 36f,
                totalTime = 36f,
                speed = 5.5f
            };

            Assert.AreEqual(1f, waveOne.bornTime);
            Assert.AreEqual(1f, waveOne.totalTime);
            Assert.AreEqual(5f, waveOne.speed);
            Assert.AreEqual(5.5f, waveTwo.speed);
        }

        [Test]
        public void LoadsFormalConfigSpeedAndTotalTime()
        {
            BaseConfig.InitAllLevel(Path.Combine(UnityEngine.Application.dataPath, "cfg.csv"));
            List<AgentConfig> configs = BaseConfig.GetLevelConfig(1);
            AgentConfig first = configs[0];
            AgentConfig waveTwo = configs.Find(config => config.spawnId == "L1_W2_RWD_01");

            Assert.AreEqual(1f, first.totalTime);
            Assert.AreEqual(5f, first.speed);
            Assert.AreEqual(5.5f, waveTwo.speed);
        }

        [Test]
        public void FindsEveryConfigDueAtTheCurrentTime()
        {
            List<AgentConfig> configs = new List<AgentConfig>
            {
                new AgentConfig { totalTime = 22f },
                new AgentConfig { totalTime = 28f },
                new AgentConfig { totalTime = 28f },
                new AgentConfig { totalTime = 36f }
            };

            Assert.AreEqual(2, CombatMgr.FindLastDueConfigIndex(configs, 0, 28f));
            Assert.AreEqual(2, CombatMgr.FindLastDueConfigIndex(configs, 2, 35f));
        }
        [Test]
        public void WritesOneDeathWaitRowForEveryElapsedSecond()
        {
            string path = Path.Combine(Path.GetTempPath(), "death-wait-log.csv");
            SessionLogger logger = SessionLogger.Instance;
            logger.CreateFile(path);
            logger.StartLevel(1);
            logger.FlushPendingEvents(0, 37, new List<AgentSnapshot>());

            logger.TickDeathWait(5f, 37);
            logger.Stop();

            string[] lines = File.ReadAllLines(path);
            Assert.AreEqual(8, lines.Length);
            for (int i = 3; i < lines.Length; i++)
            {
                string[] cells = lines[i].Split(',');
                Assert.AreEqual((i - 1).ToString(), cells[0]);
                Assert.AreEqual("", cells[4]);
                Assert.AreEqual("", cells[5]);
                Assert.AreEqual("0", cells[6]);
                Assert.AreEqual("0", cells[7]);
                Assert.AreEqual("0", cells[8]);
                Assert.AreEqual("0", cells[9]);
                Assert.AreEqual("death_wait", cells[10]);
                Assert.AreEqual("0", cells[13]);
                Assert.AreEqual("37", cells[14]);
                Assert.AreEqual("0", cells[17]);
                Assert.AreEqual("", cells[18]);
                Assert.AreEqual("", cells[19]);
            }
        }
        [Test]
        public void DeathWaitConsumesOnlyItsRemainingWindow()
        {
            Assert.AreEqual(0.5f, CombatMgr.GetDeathWaitStep(0.5f, 1f));
            Assert.AreEqual(0.25f, CombatMgr.GetDeathWaitStep(5f, 0.25f));
        }

        [Test]
        public void DetectsOnlyFutureUnprocessedConfigs()
        {
            List<AgentConfig> configs = new List<AgentConfig>
            {
                new AgentConfig { totalTime = 105f },
                new AgentConfig { totalTime = 107f },
                new AgentConfig { totalTime = 112f }
            };

            Assert.IsTrue(CombatMgr.HasFutureConfig(configs, 0, 106f));
            Assert.IsFalse(CombatMgr.HasFutureConfig(configs, 1, 112f));
        }


    }
}
