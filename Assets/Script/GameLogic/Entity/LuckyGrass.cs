using UnityEngine;

namespace Miner.GameLogic
{
    public class LuckyGrass:Reward
    {
        public override string GetPrefabPath()
        {
            return ResConst.rewardPath;
        }

        public override float GenerateHp()
        {
            return 20;
        }

        public override int GeneratePoint()
        {
            return 20;
        }
        
    }
}