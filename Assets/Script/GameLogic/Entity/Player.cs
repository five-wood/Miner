using UnityEngine;

namespace Miner.GameLogic
{
    public class Player:BaseEntity
    {
        public PlayerController playCtrl;
      
        public override string GetPrefabPath()
        {
            return ResConst.playerPath;
        }

        public Player()
        {
            playCtrl = go.AddComponent<PlayerController>();
            go.AddComponent<PlayerCollisionComp>();
        }

        public void ExitGame()
        {
         
        }

    }
}