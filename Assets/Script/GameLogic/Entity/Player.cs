using UnityEngine;

namespace Miner.GameLogic
{
    public class Player:BaseEntity
    {
        public PlayerController playCtrl;
        public LineRenderer hookLine;
        public float HOOK_MOVE_SPEED = 3.5f;
        public float HOOK_CATCH_RADIUS = 5;   //抓取半径
        public Vector3 targetHookPos;
        public float catchTime = 0;
        public float totalCatchTime = 0;
         
        public override string GetPrefabPath()
        {
            return ResConst.playerPath;
        }

        public Player()
        {
            playCtrl = go.AddComponent<PlayerController>();
            go.AddComponent<PlayerCollisionComp>();
            hookLine = go.transform.Find("hook").GetComponent<LineRenderer>();
        }

        public override void Update(float deltaTime)
        {
            if (totalCatchTime>0 && totalCatchTime >= catchTime)
            {
                float halfTime = 0.5f * totalCatchTime;
                float t = Mathf.Clamp(catchTime / halfTime, 0, 2);
                if (catchTime > halfTime)
                    t = 2 - t;
                Vector3 playerPos = go.transform.position;
                Vector3 hookPos = Vector3.Lerp(playerPos, targetHookPos, t);
                hookLine.SetPosition(1, hookPos);
                catchTime += deltaTime;
            }
            else
            {
                totalCatchTime = 0;
                catchTime = 0;
            }
        }

        public void ExitGame()
        {
         
        }

        public void Catch(Vector3 targetPos)
        {
            if (catchTime>0 && catchTime <= totalCatchTime)
            {
                return;
            }
            //播放抓取动作
            Vector3 playerPos = go.transform.position;
            Vector3 curPos = hookLine.GetPosition(1);
            targetHookPos = Vector3.MoveTowards(playerPos, targetPos, HOOK_CATCH_RADIUS);
            totalCatchTime = 2 * Vector3.Distance(curPos, targetHookPos) / HOOK_MOVE_SPEED;
            catchTime = 0;
            Debug.Log("開始抓取 totalMoveTime="+ totalCatchTime);

        }
    }
}