using UnityEngine;

namespace Miner.GameLogic
{
    public class FatMushroom:Coactive
    {
        public override string GetPrefabPath()
        {
            return ResConst.fatMushroom;
        }

    }
}