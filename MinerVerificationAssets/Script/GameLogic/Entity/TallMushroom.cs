using UnityEngine;

namespace Miner.GameLogic
{
    public class TallMushroom:Coactive
    {
        public override string GetPrefabPath()
        {
            return ResConst.tallMushroom;
        }

    }
}