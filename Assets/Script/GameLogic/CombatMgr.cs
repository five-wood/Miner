using UnityEngine.UI;
using UnityEngine;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using Miner.UI;
namespace Miner.GameLogic
{
    public class CombatMgr
    {
        private static CombatMgr _instance;
        public Player player;
        private GameObject _sceneGo;
        public BaseConfig config;
        public bool isGameOver = false;
        public float gameTime = 0;
        public int round = 0;

        public static float anchorX = -48.2f;
        public static float anchorY = 26.2f;
        public static List<Vector3> itemBornPos = new List<Vector3>(){
            new Vector3(anchorX, anchorY, 0),
            new Vector3(-anchorX, anchorY, 0),
            new Vector3(anchorX, -anchorY, 0),
            new Vector3(-anchorX, -anchorY, 0),
        };


        private MainView _mainView;
        
        public MainView mainView{
            get{
                if(_mainView == null)
                {
                    _mainView = GameObject.Find("Canvas/MainView").GetComponent<MainView>();
                }
                return _mainView;
            }
        }
        public Dictionary<int, BaseEntity> entityDict = new Dictionary<int, BaseEntity>();

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

        public void LoadGame(int level = 1)
        {
            Debug.Log("Start Game");
            this.gameTime = 0;
            this.round = 0;
            this.isGameOver = false;
            LoadConfig(level);
            CreatePlayer();
        }

        public void UpdateGame(float deltaTime)
        {
            if(!IsPlayingGame())
            {
                return;
            }
            this.gameTime += deltaTime;
            while(this.gameTime >= (this.round+1) * this.config.roundTime)
            {
                this.round++;
                this.BeginNewRound();
            }
            foreach(var entity in entityDict)
            {
                entity.Value.Update(deltaTime);
            }
            if(mainView != null)
            {
                //更新血条
                mainView.HpSlider.value = Math.Max(0, player.hp)/100.0f;
                // Debug.Log("HpSlider.value="+mainView.HpSlider.value);
                //更新积分
                mainView.pointText.text = string.Format("point：{0}", Math.Max(0, player.point));  
            }
            if(player.hp<=0)
            {
                OnGameOver();
            }
        }

        public bool IsPlayingGame()
        {
            return !(isGameOver || this.config == null);
        }

        public void BeginNewRound()
        {
            //4条路分别创建对象
            for(int i = 0; i < 4; i++)
            {
                MoveableEntity item = CreateItem();
                item.SetPosition(itemBornPos[i]);
                if(player!=null)
                {
                    Vector3 targetPos = player.GetPosition();
                    item.SetTargetPos(targetPos, UnityEngine.Random.Range(this.config.moveSpeed*0.8f, this.config.moveSpeed*1.2f));
                }
            }
        }

        public MoveableEntity CreateItem()
        {
            MoveableEntity entity = null;
            int random = UnityEngine.Random.Range(0, 60);
            if(random < 20)
            {
                if(random < 10)
                {
                    entity = new FatMushroom();
                }
                else
                {
                    entity = new TallMushroom();
                }
            }
            else if(random<40)
            {
                entity = new ToxicVine();
            }
            else
            {
                entity = new LuckyGrass();
            }
            entityDict.Add(entity.Id, entity);
            return entity;
        }


        public void LoadConfig(int level = 1)
        {
            Debug.Log("Load Config");
            if(level == 1)
            {
                this.config = new Config1();
                this.config.InitConfig();
            }
        }

        public void CreatePlayer()
        {
            Debug.Log("Create Player");
            //加载预设
            player = new Player();
            player.SetPosition(new Vector3(0, 0, 0));
            entityDict.Add(player.Id, player);
        }

        public void OnGameOver()
        {
            isGameOver = true;
            Debug.Log("Exit Game");
            //销毁游戏节点
            player.ExitGame();
            foreach(var kv in entityDict)
            {
                kv.Value.Destroy();
            }
            entityDict.Clear();
            Resources.UnloadUnusedAssets();
            if(mainView != null)
            {
                mainView.OnGameOver(false, player.hp>0);
            }
        }

        public BaseEntity GetEntityByID(int id)
        {
            return entityDict.ContainsKey(id)?entityDict[id]:null;
        }

        public void HitPlayer(int entityId)
        {
            Debug.Log("Hit Player "+entityId);
            BaseEntity entity = GetEntityByID(entityId);
            if(entity != null)
            {
                player.OnHit(entity as MoveableEntity);
                entity.Destroy();
                entityDict.Remove(entityId);
            }
        }



        public void TryCatchItem(Vector3 targetPos)
        {
            if(IsPlayingGame())
            {
                player.Catch(targetPos);
            }
        }

        public void OnSuccessCatch(int entityId)
        {
            // Debug.Log("OnSuccessCatch");
            MoveableEntity entity = GetEntityByID(entityId) as MoveableEntity;
            if(entity != null)
            {
                player.OnSuccessCatch(entityId);
            }
        }

        public void OnProtectSuccess(int entityId)
        {
            //保护成功
            MoveableEntity entity = GetEntityByID(entityId) as MoveableEntity;
            if(entity != null)
            {
                entity.Destroy();
                entityDict.Remove(entityId);
            }
        }

        public void ProtectPlayer()
        {
            player.Protect();
        }

    }

}