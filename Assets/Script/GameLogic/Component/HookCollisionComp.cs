using UnityEngine;
namespace Miner.GameLogic
{
    public class HookCollisionComp:MonoBehaviour
    {
        Collider collider;
        void Start()
        {
            collider = EntityUtils.CreateCollider(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            IDComp idComp = other.gameObject.GetComponent<IDComp>();
            if(idComp != null)
            {
                // Debug.Log("OnSuccessCatch "+idComp.id);
                CombatMgr.Instance().OnSuccessCatch(idComp.id);
            }
        }

        public void Disable()
        {
            if(collider)
            {
                collider.enabled = false;
            }
        }

        public void Enable()
        {
            if(collider)
            {
                collider.enabled = true;
            }
        }
    }
}