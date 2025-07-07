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

        private static float intervalDuration = 0;
        public void Update()
        {
            intervalDuration -= Time.deltaTime;

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
  
            if (Input.GetKey(KeyCode.LeftControl)&& Input.GetKey(KeyCode.P) )
            {
                if(intervalDuration<=0)
                {
                    CombatMgr.Instance().Pause();
                    intervalDuration = 1;
                }
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetKey(KeyCode.Alpha1))
                {
                    if (intervalDuration <= 0)
                    {
                        CombatMgr.Instance().ForceEnter(1);
                        intervalDuration = 1;
                    }
                }
                else if(Input.GetKey(KeyCode.Alpha2))
                {
                    if (intervalDuration <= 0)
                    {
                        CombatMgr.Instance().ForceEnter(2);
                        intervalDuration = 1;
                    }
                }
                else if (Input.GetKey(KeyCode.Alpha3))
                {
                    if (intervalDuration <= 0)
                    {
                        CombatMgr.Instance().ForceEnter(3);
                        intervalDuration = 1;
                    }
                }
                else if (Input.GetKey(KeyCode.Alpha4))
                {
                    if (intervalDuration <= 0)
                    {
                        CombatMgr.Instance().ForceEnter(4);
                        intervalDuration = 1;
                    }
                }
                else if (Input.GetKey(KeyCode.Alpha5))
                {
                    if (intervalDuration <= 0)
                    {
                        CombatMgr.Instance().ForceEnter(5);
                        intervalDuration = 1;
                    }
                }
                else if (Input.GetKey(KeyCode.Alpha6))
                {
                    if (intervalDuration <= 0)
                    {
                        CombatMgr.Instance().ForceEnter(6);
                        intervalDuration = 1;
                    }
                }
                else if (Input.GetKey(KeyCode.Alpha7))
                {
                    if (intervalDuration <= 0)
                    {
                        CombatMgr.Instance().ForceEnter(7);
                        intervalDuration = 1;
                    }
                }
                else if (Input.GetKey(KeyCode.Alpha8))
                {
                    if (intervalDuration <= 0)
                    {
                        CombatMgr.Instance().ForceEnter(8);
                        intervalDuration = 1;
                    }
                }
                else if (Input.GetKey(KeyCode.Alpha9))
                {
                    if (intervalDuration <= 0)
                    {
                        CombatMgr.Instance().ForceEnter(9);
                        intervalDuration = 1;
                    }
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