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
                //Debug.Log("click worldPos " + worldPos.ToString());
                XLogger.Info("Throw the hook, clickPos =" + worldPos.ToString());
                CombatMgr.Instance().TryCatchItem(worldPos);
            }
            if (Input.GetMouseButtonDown(1))
            {
                Vector3 screenPos = Input.mousePosition;
                screenPos.z = -1* Camera.main.transform.position.z;  
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                //Debug.Log("click worldPos " + worldPos.ToString());
                XLogger.Info("Throw the Shield, clickPos = " + worldPos.ToString());
                CombatMgr.Instance().ProtectPlayer(worldPos);
            }
  
            if (Input.GetKeyDown(KeyCode.LeftControl)&& Input.GetKeyDown(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Q))
            {
                CombatMgr.Instance().Pause();
            }
            if (Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.LeftAlt))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    CombatMgr.Instance().ForceEnter(1);
                }
                else if(Input.GetKeyDown(KeyCode.Alpha2))
                {
                    CombatMgr.Instance().ForceEnter(2);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    CombatMgr.Instance().ForceEnter(3);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    CombatMgr.Instance().ForceEnter(4);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    CombatMgr.Instance().ForceEnter(5);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    CombatMgr.Instance().ForceEnter(6);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha7))
                {
                    CombatMgr.Instance().ForceEnter(7);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha8))
                {
                    CombatMgr.Instance().ForceEnter(8);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha9))
                {
                    CombatMgr.Instance().ForceEnter(9);
                }
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