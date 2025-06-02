using UnityEngine;

namespace Miner.GameLogic
{
    public class ToxicVine:Threat
    {
        public override string GetPrefabPath()
        {
            return ResConst.threatPath;
        }

        public override float GenerateHp()
        {
            return -2;
        }

        public override int GeneratePoint()
        {
            return -20;
        }

    }
}