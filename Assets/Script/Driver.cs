using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Miner.GameLogic;
using Miner.Utils;

public class Driver : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Screen.SetResolution(1920, 1080, true);
        string cfgPath = Application.dataPath + "/cfg.csv";
        BaseConfig.InitAllLevel(cfgPath);

        string logFileName = string.Format("log_{0}_{1}.csv", System.DateTime.Now.ToShortDateString(), System.DateTime.Now.ToShortTimeString()).Replace("/", "_").Replace(":", "_");
        Debug.Log("logFileName " + logFileName);
        string logPath = string.Format("{0}/{1}", Application.dataPath, logFileName);
        Debug.Log("logPath "+logPath);
        SessionLogger.Instance.CreateFile(logPath);
    }

    // Update is called once per frame
    void Update()
    {
        CombatMgr.Instance().UpdateGame(Time.deltaTime);
        InputMgr.Instance().Update();
;   }

    void OnEnable()
    {
        Application.quitting += OnQuitting;
    }

    void OnDisable()
    {
        Application.quitting -= OnQuitting;
    }

    void OnQuitting()
    {
        Debug.Log("游戏即将退出");
        SocketManager.Instance.Disconnect();
        CombatMgr combat = CombatMgr.Instance();
        if (combat.player != null)
        {
            SessionLogger.Instance.EndLevel((int)combat.player.hp, combat.player.point, combat.CollectAgentSnapshots());
        }
        SessionLogger.Instance.Stop();
    }


}
