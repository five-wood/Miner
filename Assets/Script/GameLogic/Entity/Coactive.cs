using UnityEngine;

namespace Miner.GameLogic
{
    public class Coactive:MoveableEntity
    {
        public override string GetPrefabPath()
        {
            return ResConst.fatMushroom;
        }
        
    }
}