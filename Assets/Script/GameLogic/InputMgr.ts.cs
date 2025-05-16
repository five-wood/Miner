using UnityEngine;

namespace Miner.GameLogic
{
    public class InputMgr
    {
        private static InputMgr _instance;
        public static InputMgr Instance()
        {
            if (_instance == null)
            {
                _instance = new InputMgr();
            }
            return _instance;
        }

        public void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 screenPos = Input.mousePosition;
                screenPos.z = -1* Camera.main.transform.position.z;  // 设定与摄像机的距离
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                Debug.Log("click worldPos " + worldPos.ToString());
                CombatMgr.Instance().CatchItem(worldPos);
            }
        }
        
    }
}