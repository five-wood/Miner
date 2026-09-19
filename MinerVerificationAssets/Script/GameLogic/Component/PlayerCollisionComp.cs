using UnityEngine;
namespace Miner.GameLogic
{
    //玩家碰撞组件
    public class PlayerCollisionComp:MonoBehaviour
    {
        void Start()
        {
            Collider collider = gameObject.GetComponent<Collider>();
            Rigidbody rigid = gameObject.GetComponent<Rigidbody>(); ;
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
                rigid = gameObject.AddComponent<Rigidbody>();
            }
            collider.isTrigger = true;
            rigid.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            IDComp idComp = other.gameObject.GetComponent<IDComp>();
            if(idComp != null)
            {
                CombatMgr.Instance().HitPlayer(idComp.id);
            }
        }

    }
}