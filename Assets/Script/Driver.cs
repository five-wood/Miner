using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Miner.GameLogic;

public class Driver : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Screen.SetResolution(1920, 1080, true);
        string cfgPath = Application.dataPath + "/cfg.csv";
        BaseConfig.InitAllLevel(cfgPath);

        string logFileName = string.Format("log_{0}_{1}.txt", System.DateTime.Now.ToShortDateString(), System.DateTime.Now.ToShortTimeString()).Replace("/", "_").Replace(":", "_");
        Debug.Log("logFileName " + logFileName);
        string logPath = string.Format("{0}/{1}", Application.dataPath, logFileName);
        Debug.Log("logPath "+logPath);
        XLogger.OpenLogFile(logPath);
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
        Debug.Log("ÓÎÏ·¼´½«ÍË³ö£¡");
        XLogger.Stop();
    }


}
