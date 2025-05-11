using UnityEngine;

namespace Miner.GameLogic
{
    public class Threat:MoveableEntity
    {
        public override string GetPrefabPath()
        {
            return ResConst.threatPath;
        }
        
    }
}