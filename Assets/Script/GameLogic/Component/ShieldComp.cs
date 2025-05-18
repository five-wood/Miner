using System;
using UnityEngine;
namespace Miner.GameLogic
{
    public class ShieldComp:MonoBehaviour
    {
        public bool changing = false;
        public float MAX_SCALE = 5f;
        public float MIN_SCALE = 1f;
        public float SCALE_CHANGE_TIME = 1f;
        public float duration = 0f;
        void Start()
        {
            EntityUtils.CreateCollider(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            IDComp idComp = other.gameObject.GetComponent<IDComp>();
            if(idComp != null)
            {
                // Debug.Log("OnSuccessCatch "+idComp.id);
                CombatMgr.Instance().OnProtectSuccess(idComp.id);
            }
        }

        public void Update()
        {
            if(changing)
            {
                duration += Time.deltaTime;
                float halfTime = 0.5f * SCALE_CHANGE_TIME;
                float t = Mathf.Clamp(duration / halfTime, 0, 2);
                if (duration > halfTime)
                    t = 2 - t;
                float scale = Mathf.Lerp(MIN_SCALE, MAX_SCALE, t);
                transform.localScale = new Vector3(scale, scale, scale);
                if(t<=0)
                {
                    changing = false;
                }
            }
        }

        public void Protect()
        {
            if(changing)
                return;
            //保护
            changing = true;
            duration = 0f;
        }
    }
}