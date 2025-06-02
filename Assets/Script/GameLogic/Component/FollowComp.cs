using UnityEngine;
namespace Miner.GameLogic
{
    public class FollowComp:MonoBehaviour
    {
        private Vector3 _targetPos;
        public Vector3 targetPos{
            get{
                return _targetPos;
            }
            set{
                _targetPos = value;
            }
        }
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