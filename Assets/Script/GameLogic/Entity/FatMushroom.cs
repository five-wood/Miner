using UnityEngine;

namespace Miner.GameLogic
{
    public class FatMushroom:Coactive
    {
        public override string GetPrefabPath()
        {
            return ResConst.fatMushroom;
        }
        
        public override float GenerateHp()
        {
            return 20;
        }

        public override int GeneratePoint()
        {
            return -20;
        }
    }
}