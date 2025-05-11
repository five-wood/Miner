using UnityEngine;

namespace Miner.GameLogic
{
    public class MoveableEntity:BaseEntity
    {
        public Vector3 targetPos;
        public float speed = 10;

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
            if(followComp != null)
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
            }
        }
    }
}