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
                screenPos.z = -1* Camera.main.transform.position.z;  
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                Debug.Log("click worldPos " + worldPos.ToString());
                CombatMgr.Instance().TryCatchItem(worldPos);
            }
            if (Input.GetMouseButtonDown(1))
            {
                Vector3 screenPos = Input.mousePosition;
                screenPos.z = -1* Camera.main.transform.position.z;  
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                Debug.Log("click worldPos " + worldPos.ToString());
                CombatMgr.Instance().ProtectPlayer(worldPos);
            }
            
            checkArrow();
        }

        //根据鼠标位置，调整出钩的箭头
        public void checkArrow()
        {
            Vector3 screenPos = Input.mousePosition;
            screenPos.z = -1* Camera.main.transform.position.z;  
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            CombatMgr.Instance().AdjustHookArrow(worldPos);
        }
    }
}