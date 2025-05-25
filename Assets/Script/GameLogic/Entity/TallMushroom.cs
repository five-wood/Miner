using UnityEngine;

namespace Miner.GameLogic
{
    public class TallMushroom:Coactive
    {
        public override string GetPrefabPath()
        {
            return ResConst.tallMushroom;
        }

        public override float GenerateHp()
        {
            return -20;
        }

        public override int GeneratePoint()
        {
            return 20;
        }
        
    }
}