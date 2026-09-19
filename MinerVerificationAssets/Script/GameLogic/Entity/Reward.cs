using UnityEngine;

namespace Miner.GameLogic
{
    public class Reward:MoveableEntity
    {
        public override string GetPrefabPath()
        {
            return ResConst.rewardPath;
        }
        
    }
}