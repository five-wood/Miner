using UnityEngine;

namespace Miner.GameLogic
{
    public class Threat:MoveableEntity
    {
        public float nextCheckDuration = 0;
        public override string GetPrefabPath()
        {
            return ResConst.threatPath;
        }
        
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            if(nextCheckDuration > 0)
            {
                nextCheckDuration -= deltaTime;
            }
            else
            {
                Player player = CombatMgr.Instance().player;
                if(player != null)
                {
                    float distance = Vector3.Distance(player.go.transform.position, go.transform.position);
                    if(distance < player.HOOK_CATCH_RADIUS)
                    {
                        //有点问题，没走进来 等待排查
                        nextCheckDuration = 0.5f;
                        IntervalHurtPlayer(player);
                    }
                }
            }
        }

        public virtual void IntervalHurtPlayer(Player player)
        {
            player.BeHurt(GenerateHp());
        }
    }
}