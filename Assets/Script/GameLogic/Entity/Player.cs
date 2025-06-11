using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Miner.GameLogic
{
    public class Player:BaseEntity
    {
        public PlayerController playCtrl;
        public LineRenderer hookLine;
        public GameObject hookHead;
        public Transform arrowTrans;
        public float HOOK_MOVE_SPEED = 15f;
        public float HOOK_CATCH_RADIUS = 25;   //半径
        public Vector3 targetHookPos;
        public float catchDuration = 0;
        public float totalCatchTime = 0;
        public ShieldComp shieldComp;

        public HookCollisionComp hookCollisionComp;

        public int catchEntityId = 0;

        public float hp = 100;
        public int point = 0;
         
        public override string GetPrefabPath()
        {
            return ResConst.playerPath;
        }

        public Player()
        {
            playCtrl = go.AddComponent<PlayerController>();
            //模型
            GameObject model = go.transform.Find("model").gameObject;
            model.AddComponent<PlayerCollisionComp>();
            //钩子
            hookLine = go.transform.Find("hook").GetComponent<LineRenderer>();
            hookHead = hookLine.gameObject.transform.Find("head").gameObject;
            hookLine.SetPosition(0, Vector3.zero);
            SetHookPos(Vector3.zero);
            hookCollisionComp = hookHead.gameObject.AddComponent<HookCollisionComp>();
            //抓取范围
            Transform circle = go.transform.Find("circle");
            circle.localScale = new Vector3(HOOK_CATCH_RADIUS*2, HOOK_CATCH_RADIUS*2, 1);
            //盾牌
            GameObject shield = go.transform.Find("shield").gameObject;
            shieldComp = shield.AddComponent<ShieldComp>();
            //箭头
            arrowTrans = go.transform.Find("arrow");
        }

        public override void Update(float deltaTime)
        {
            //抓到道具
            if(catchEntityId != 0)
            {
                catchDuration += deltaTime;
                float t = Mathf.Clamp(catchDuration / totalCatchTime, 0, 1);
                Vector3 playerPos = go.transform.position;
                Vector3 hookPos = Vector3.Lerp(targetHookPos, playerPos, t);
                SetHookPos(hookPos);
                BaseEntity catchEntity = CombatMgr.Instance().GetEntityByID(catchEntityId);
                if(catchEntity != null)
                {
                    catchEntity.go.transform.localPosition = new Vector3(2,0,0);
                }
                if(t>=1) //到手
                {
                    catchEntityId = 0;
                    totalCatchTime = 0;
                    catchDuration = 0;
                    hookLine.gameObject.SetActive(false);
                }
            }
            else //还没抓到道具
            {
                if (totalCatchTime>0 && totalCatchTime >= catchDuration)
                {
                    catchDuration += deltaTime;
                    float halfTime = 0.5f * totalCatchTime;
                    float t = Mathf.Clamp(catchDuration / halfTime, 0, 2);
                    if (catchDuration > halfTime)
                        t = 2 - t;
                    Vector3 playerPos = go.transform.position;
                    Vector3 hookPos = Vector3.Lerp(playerPos, targetHookPos, t);
                    SetHookPos(hookPos);

                }
                else
                {
                    totalCatchTime = 0;
                    catchDuration = 0;
                    hookLine.gameObject.SetActive(false);
                }
            }
        }

        public void ExitGame()
        {
         
        }

        //抓取
        public void Catch(Vector3 targetPos)
        {
            // Debug.Log("Catch "+targetPos + " catchDuration="+catchDuration + " totalCatchTime="+totalCatchTime);
            if(IsCasting())
                return;
            Vector3 playerPos = go.transform.position;
            Vector3 curPos = hookLine.GetPosition(1);
            Vector3 dir = (targetPos - playerPos).normalized;
            targetHookPos = playerPos + dir * HOOK_CATCH_RADIUS;
            totalCatchTime = 2 * Vector3.Distance(curPos, targetHookPos) / HOOK_MOVE_SPEED;
            catchDuration = 0;
            hookLine.gameObject.SetActive(true);
            hookCollisionComp.Enable();
            SetHookRotation();
        }
        public void Protect(Vector3 pos)
        {
            if(IsCasting())
                return;
            //保护
            if(this.shieldComp != null)
            {
                Vector3 playerPos = go.transform.position;
                Vector3 dir = (pos - playerPos).normalized;
                Vector3 tmpPos = playerPos + dir * HOOK_CATCH_RADIUS;
                this.shieldComp.Protect(tmpPos);
            }
        }
        //正在出钩或正在出盾
        public bool IsCasting()
        {
             //出钩抓取中，不可重复抓
            if (IsHookFlying())
            {
                return true;
            }
            //盾牌飞行中，不可出钩
            if(this.shieldComp!=null && this.shieldComp.IsFlying())
            {
                return true;
            }
            return false;
        }

        public bool IsHookFlying()
        {
            return catchDuration>0 && catchDuration <= totalCatchTime;
        }

        //设置钩子位置
        public void SetHookPos(Vector3 pos)
        {
            //pos
            hookLine.SetPosition(1, pos);
            hookHead.transform.position = pos;
        }

        public void SetHookRotation()
        {
            //rotation
            Vector3 playerPos = go.transform.position;
            Vector3 dir = targetHookPos - playerPos;
            //根据originPos和dir计算出hookHead的z轴旋转
            float angle = Vector3.Angle(new Vector3(1,0,0), dir);
            if(dir.y<0)
            {
                angle = -angle;
            }
            // Debug.LogError("dir="+dir+" angle="+angle);
            hookHead.transform.localEulerAngles = new Vector3(0, 0, angle);
        }

        public void OnSuccessCatch(int entityId)
        {
            //抓住item
            MoveableEntity entity = CombatMgr.Instance().GetEntityByID(entityId) as MoveableEntity;
            if(entity != null)
            {
                hookCollisionComp.Disable();
                catchEntityId = entityId;
                // entity.go.GetComponent<Collider>().enabled = false;
                entity.go.transform.SetParent(hookHead.transform);
                entity.go.transform.localPosition = new Vector3(2,0,0);
                targetHookPos = entity.go.transform.position;
                Vector3 playerPos = go.transform.position;
                totalCatchTime = Vector3.Distance(targetHookPos, playerPos) / HOOK_MOVE_SPEED;
                catchDuration = 0;
            }
        }

        public void OnHit(MoveableEntity entity)
        {
            if(!EntityUtils.IsReward(entity)||catchEntityId == entity.Id)
            {
                float hpChanged = entity.GenerateHp();
                CombatMgr.Instance().ChangeHp(hpChanged);
                hp = Mathf.Clamp(hp+hpChanged, 0, 100);
                int pointChange = entity.GeneratePoint();
                point += Math.Max(pointChange, 0);
                CombatMgr.Instance().ChangePoint(pointChange);
            }
            if (entity!=null)
            {
                entity.Destroy();
            }
        }

        public void BeHurt(float damage)
        {
            hp = Mathf.Clamp(hp+damage, 0, 100);
            CombatMgr.Instance().ChangeHp(damage);
        }


        public void AdjustArrow(Vector3 pos)
        {
            if(IsCasting())
                return;
            Vector3 playerPos = go.transform.position;
            Vector3 dir = pos - playerPos;
            //根据originPos和dir计算出与z轴旋转
            float angle = Vector3.Angle(new Vector3(1,0,0), dir);
            if(dir.y<0)
            {
                angle = -angle;
            }
            arrowTrans.localEulerAngles = new Vector3(0, 0, angle-90);
        }
    }
}