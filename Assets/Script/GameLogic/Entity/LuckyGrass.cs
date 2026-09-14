using UnityEngine;

namespace Miner.GameLogic
{
    public class LuckyGrass:Reward
    {
        public override string GetPrefabPath()
        {
            return ResConst.rewardPath;
        }

    }
}