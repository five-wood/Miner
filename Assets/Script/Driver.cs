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
        BaseConfig.InitAllLevel("Assets/Conf/cfg.csv");
    }

    // Update is called once per frame
    void Update()
    {
        CombatMgr.Instance().UpdateGame(Time.deltaTime);
        InputMgr.Instance().Update();
;    }
}
