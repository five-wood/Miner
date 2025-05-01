using UnityEngine.UI;
using UnityEngine;
namespace Miner.GameLogic
{
    public class CombatMgr
    {
        private static CombatMgr _instance;
        public Player player;
        private GameObject _sceneGo;
        public GameObject sceneGo{
            get{
                if(_sceneGo == null)
                {
                    _sceneGo = GameObject.Find("scene");
                }
                return _sceneGo;
            }
        }
        public static CombatMgr Instance()
        {
            if (_instance == null)
            {
                _instance = new CombatMgr();
            }
            return _instance;
        }

        public void LoadGame()
        {
            Debug.Log("Start Game");
            LoadPlayer();
        }

        public void LoadPlayer()
        {
            Debug.Log("Load Player");
            //加载预设
            GameObject perfab = Resources.Load<GameObject>(ResConst.playerPath);
            GameObject go = GameObject.Instantiate(perfab);
            go.transform.SetParent(sceneGo.transform);
            player = new Player(go);
            player.SetPosition(new Vector3(0, 0, 0));
        }

        public void ExitGame()
        {
            Debug.Log("Exit Game");
            //销毁游戏节点
            player.ExitGame();
            Resources.UnloadUnusedAssets();
        }
    }

}