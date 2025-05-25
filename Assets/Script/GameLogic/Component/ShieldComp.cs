using System;
using UnityEngine;
namespace Miner.GameLogic
{
    public class ShieldComp:MonoBehaviour
    {
        public bool changing = false;
        public float MAX_SCALE = 1.6f;
        public float MIN_SCALE = 0f;
        public float SCALE_CHANGE_TIME = 0.7f;
        public float duration = 0f;

        void Start()
        {
            EntityUtils.CreateCollider(gameObject);
            transform.localScale = new Vector3(0,0,0);
            float spriteScale = 1f;
            var spriteRenderer = transform.GetComponent<SpriteRenderer>();
            if(spriteRenderer != null)
            {
                spriteScale = spriteRenderer.sprite.bounds.size.x;
                // Debug.LogError("spritaScale "+spritaScale);
            }
            BoxCollider collider = getCollider();
            collider.size = new Vector3(spriteScale, spriteScale, spriteScale);
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

        private BoxCollider _collider;
        private BoxCollider getCollider()
        {
            if(_collider==null)
            {
                _collider = transform.GetComponent<BoxCollider>();
            }
            return _collider;
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