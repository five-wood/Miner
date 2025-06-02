using System;
using UnityEngine;
namespace Miner.GameLogic
{
    public class ShieldComp:MonoBehaviour
    {
        public float SHIELD_MOVE_SPEED = 50f;
        public float duration = 0f;
        public float totalTime = 0f;

        public Vector3 targetPos;

        void Start()
        {
            EntityUtils.CreateCollider(gameObject);
            transform.localScale = new Vector3(1,1,1);
            float spriteScale = 1f;
            var spriteRenderer = transform.GetComponent<SpriteRenderer>();
            if(spriteRenderer != null)
            {
                spriteScale = spriteRenderer.sprite.bounds.size.x * 0.5f;
                // Debug.LogError("spritaScale "+spritaScale);
            }
            BoxCollider collider = getCollider();
            collider.size = new Vector3(spriteScale, spriteScale, spriteScale);
            gameObject.SetActive(false);
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
            OnUpdatePosition();
        }

        public void OnUpdatePosition()
        {
            if(totalTime>0)
            {
                duration += Time.deltaTime;
                float t = Mathf.Clamp(duration / totalTime, 0, 1);

                transform.position = Vector3.Lerp(GetPlayerPos(), targetPos, t);
                if(duration>=totalTime)
                {
                    totalTime = -1;
                    duration = 0;
                    gameObject.SetActive(false);
                }
            }
        }

        public void Protect(Vector3 pos)
        {
            if(totalTime>0)
            {
                return;
            }
            targetPos = pos;
            duration = 0f;
            Vector3 playerPos = GetPlayerPos();
            gameObject.transform.position = playerPos;
            gameObject.SetActive(true);
            totalTime = Vector3.Distance(playerPos, targetPos) / SHIELD_MOVE_SPEED;
            Vector3 originScale = gameObject.transform.localScale;
            SetRotation(targetPos);
        }

        public Vector3 GetPlayerPos()
        {
            if(CombatMgr.Instance().player != null)
            {
                return CombatMgr.Instance().player.go.transform.position;
            }
            return Vector3.zero;
        }

        
        public void SetRotation(Vector3 pos)
        {
            //rotation
            Vector3 playerPos = GetPlayerPos();
            Vector3 dir = pos - playerPos;
            //根据originPos和dir计算出hookHead的z轴旋转
            float angle = Vector3.Angle(new Vector3(1,0,0), dir);
            if(dir.y<0)
            {
                angle = -angle;
            }
            // Debug.LogError("dir="+dir+" angle="+angle);
            transform.localEulerAngles = new Vector3(0, 0, angle);
        }
    }
}