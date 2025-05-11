using UnityEngine;
namespace Miner.GameLogic
{
    public class FollowComp:MonoBehaviour
    {
        public Vector3 targetPos;
        public float speed = 10;

        public void SetSpeed(float speed)
        {
            this.speed = speed;
        }
        public void UpdateDelta(float deltaTime)
        {
            if(targetPos != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            }
        }
    }
}