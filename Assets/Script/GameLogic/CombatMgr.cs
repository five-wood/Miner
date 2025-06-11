using UnityEngine.UI;
using UnityEngine;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using Miner.UI;
using Unity.VisualScripting;
namespace Miner.GameLogic
{
    public class CombatMgr
    {
        private static CombatMgr _instance;
        public Player player;
        private GameObject _sceneGo;
        public bool isGameOver = true;
        public float gameTime = 0;
        public int level = 1;
        private int lastAgentIndex = -1;

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
            this.lastAgentIndex = -1;
            this.isGameOver = false;
            this.level = level;
            CreatePlayer();
        }

        private List<int> _destroyList = new List<int>();
        public void UpdateGame(float deltaTime)
        {
            if(!IsPlayingGame())
            {
                return;
            }

            //检查是否需要创建新的怪物
            this.gameTime += deltaTime;
            this.CheckRound();
            
            //处理延迟销毁的实体
            _destroyList.Clear();
            foreach(var entityKV in entityDict)
            {
                BaseEntity entity = entityKV.Value;
                entity.Update(deltaTime);
                if(entity.delayDestoryTime > 0)
                {
                    entity.delayDestoryTime -= deltaTime;
                    if(entity.delayDestoryTime <= 0)
                    {
                        entity.delayDestoryTime = 0;
                        _destroyList.Add(entityKV.Key);
                    }
                }
            }
            for(int i = 0; i < _destroyList.Count; i++)
            {
                entityDict[_destroyList[i]].Destroy();
                entityDict.Remove(_destroyList[i]);
            }

            //更新主界面
            if(mainView != null)
            {
                //更新血条
                mainView.HpSlider.value = Math.Max(0, player.hp)/100.0f;
                // Debug.Log("HpSlider.value="+mainView.HpSlider.value);
                //更新积分
                mainView.pointText.text = string.Format("{0}", Math.Max(0, player.point));  
            }

            //血量见底，游戏结束
            if(player.hp<=0)
            {
                OnGameOver();
            }
        }

        public bool IsPlayingGame()
        {
            return !isGameOver;
        }

        public void CheckRound()
        {
            //4条路分别创建对象
            List<AgentConfig> agentConfigs = BaseConfig.GetLevelConfig(this.level);
            if(this.lastAgentIndex >= agentConfigs.Count-1)
            {
                // Debug.LogError("本关卡结束，所有怪物都已经创建完毕");
                return;
            }
            AgentConfig agentConfig = agentConfigs[this.lastAgentIndex+1];
            if(this.gameTime >= agentConfig.bornTime)
            {
                //Debug.Log(this.lastAgentIndex+" Create Item "+agentConfig.agentName+" ,cnt = "+agentConfigs.Count);
                this.lastAgentIndex++;
                CreateItem(agentConfig);
            }
        }

        public MoveableEntity CreateItem(AgentConfig agentConfig)
        {
            MoveableEntity entity = null;
            switch(agentConfig.agentName)
            {
                case "Lucky grass":
                    entity = new LuckyGrass();
                    break;
                case "Toxic Vine":
                    entity = new FatMushroom();
                    break;
                case "Tall Mushroom":
                    entity = new TallMushroom();
                    break;
                case "Fat Mushroom":
                    entity = new FatMushroom();
                    break;
                default:
                    Debug.LogError("CreateItem error: "+agentConfig.agentName);
                    break;
            }
            entityDict.Add(entity.Id, entity);
            entity.SetPosition(itemBornPos[(int)agentConfig.posType]);
            if(player!=null)
            {
                Vector3 targetPos = player.GetPosition();
                entity.SetTargetPos(targetPos, agentConfig.speed);
            }
            return entity;
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
                if(entityDict.ContainsKey(entityId))
                {
                    entityDict.Remove(entityId);
                }
            }
        }

        public void TryCatchItem(Vector3 targetPos)
        {
            if(player == null) return;
            if(IsPlayingGame())
            {
                player.Catch(targetPos);
            }
        }
        public void ProtectPlayer(Vector3 pos)
        {
            if(player == null) return;
            player.Protect(pos);
        }

        public void AdjustHookArrow(Vector3 pos)
        {
            if(player == null) return;
            player.AdjustArrow(pos);
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
                Vector3 playerPos = player.GetPosition();
                Vector3 entityPos = entity.GetPosition();
                float x = entityPos.x>playerPos.x?1:-1;
                float y = entityPos.y>playerPos.y?1:-1;
                Vector3 pos = playerPos+new Vector3(x*100, y*100, entityPos.z);
                entity.BeHitAway(pos);
                // entity.Destroy();
                // entityDict.Remove(entityId);
            }
        }



    }

}