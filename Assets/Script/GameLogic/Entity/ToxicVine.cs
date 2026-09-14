using UnityEngine;

namespace Miner.GameLogic
{
    public class ToxicVine:Threat
    {
        public BulletComp bulletComp;
        public ToxicVine()
        {
            bulletComp = go.AddComponent<BulletComp>();
        }

        public override void IntervalHurtPlayer(Player player)
        {
            //Debug.Log("IntervalHurtPlayer");
            base.IntervalHurtPlayer(player);
            bulletComp.Shot(player.go.transform.position, go.transform.position,()=> {
                if(player!=null && !player.isDestroy)
                {
                    player.BeHurt(Stats().shotHp, this) ;
                }
            });
        }

        public override string GetPrefabPath()
        {
            return ResConst.threatPath;
        }

        public override void Destroy()
        {
            base.Destroy();
            bulletComp.Destroy();
        }

    }
}