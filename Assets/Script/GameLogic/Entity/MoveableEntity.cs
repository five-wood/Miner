using UnityEngine;

namespace Miner.GameLogic
{
    public class MoveableEntity:BaseEntity
    {
        public Vector3 targetPos;
        public float speed = 50;
        public bool beCaught = false;

        public FollowComp followComp;

        public MoveableEntity()
        {
            followComp = go.GetComponent<FollowComp>();
            if(followComp == null)
            {
                followComp = go.AddComponent<FollowComp>();
            }
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            if(followComp != null && !beCaught)
            {
                followComp.UpdateDelta(deltaTime);
            }
        }

        public virtual void SetTargetPos(Vector3 pos, float speed)
        {
            targetPos = pos;
            if(followComp != null)
            {
                followComp.SetSpeed(speed);
                followComp.targetPos = pos;
            }
        }

        public void OnBeCaught()
        {
            Debug.Log("OnBeCaught "+Id);
            beCaught = true;
        }

        public virtual float GenerateHp()
        {
            return 0;
        }

        public virtual int GeneratePoint()
        {
            return 0;
        }

        //被击飞
        public void BeHitAway(Vector3 targetPos)
        {
            if(followComp != null)
            {
                followComp.SetSpeed(speed*2);
                followComp.targetPos = targetPos;
                delayDestoryTime = 3.0f;
            }
        }

    }
}