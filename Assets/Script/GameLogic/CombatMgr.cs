using UnityEngine.UI;
using UnityEngine;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using Miner.UI;
using Miner.Utils;
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
        public int wave = 0;
        private bool pause = false;

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
            if(level>BaseConfig.maxLevel)
            {
                Debug.LogError("传入的关卡超上限");
                return;
            }
            this.gameTime = 0;
            this.lastAgentIndex = -1;
            this.isGameOver = false;
            this.level = level;
            this.pause = false;
            this.wave = 0;
            CreatePlayer();
            SessionLogger.Instance.StartLevel(this.level);
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
            //Debug.LogError("deltaTime = " + deltaTime + " , gameTime " + this.gameTime + "frameCoutn "+Time.frameCount);
            bool hadNextAgent = this.CheckRound();
            
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

            if (player != null && !player.isDestroy)
            {
                SessionLogger.Instance.Tick(deltaTime, (int)player.hp, player.point, CollectAgentSnapshots());
            }

            //更新主界面
            if(mainView != null)
            {
                //更新血条
                mainView.HpSlider.value = Math.Max(0, player.hp)/100.0f;
                // Debug.Log("HpSlider.value="+mainView.HpSlider.value);
                //更新积分
                mainView.pointText.text = string.Format("{0}", player.point);
            }

            //血量见底失败,或者所有agent消失只剩玩家
            if (player.hp<=0 || (!hadNextAgent && entityDict.Count == 1))
            {
                OnGameOver();
            }
        }

        public void ChangeHp(float hp)
        {
            if (mainView != null)
            {
                mainView.ChangeHp(hp);
            }
        }

        public void ChangePoint(int point)
        {
            if (mainView != null)
            {
                mainView.ChangePoint(point);
            }
        }

        public bool IsPlayingGame()
        {
            return !isGameOver && !pause;
        }

        public bool CheckRound()
        {
            List<AgentConfig> agentConfigs = BaseConfig.GetLevelConfig(this.level);
            if(this.lastAgentIndex >= agentConfigs.Count-1)
            {
                // Debug.LogError("本关卡结束，所有怪物都已经创建完毕");
                return false;
            }
            AgentConfig agentConfig = agentConfigs[this.lastAgentIndex+1];
            if(this.gameTime >= agentConfig.bornTime)
            {
                XLogger.Info("Create Agent ["+agentConfig.agentName+"],gameTime ="+ this.gameTime+"s");
                this.lastAgentIndex++;
                CreateItem(agentConfig);
                this.wave = agentConfig.wave;
            }
            return true;
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
                    entity = new ToxicVine();
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
            entity.name = agentConfig.agentName;
            entity.InitConfig(agentConfig);
            entityDict.Add(entity.Id, entity);
            entity.SetPosition(itemBornPos[(int)agentConfig.posType]);
            if(player!=null)
            {
                Vector3 targetPos = player.GetPosition();
                entity.SetTargetPos(targetPos, agentConfig.speed);
            }
            entity.logEnteredAt = this.gameTime;
            bool duplicate = SessionLogger.Instance.RegisterSpawnId(agentConfig.spawnId);
            SessionLogger.Instance.Enqueue(AgentEvent("entry", entity, 0, 0, true, duplicate));
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
            bool isWin = player.hp > 0;
            if (isWin)
            {
                SessionLogger.Instance.EndLevel((int)player.hp, player.point, CollectAgentSnapshots());
            }
            int score = player.point;
            
            if(isWin)
            {
                XLogger.Info(string.Format("game win, run={2}, hp={0},score={1}", player.hp, player.point, this.level));
            }
            else
            {
                var agentConfigs = BaseConfig.GetLevelConfig(this.level);
                AgentConfig config = agentConfigs[this.lastAgentIndex];
                XLogger.Info(string.Format("player died, run={2}, hp={0}, score={1}, wave={3}", player.hp, player.point, this.level, config.wave));
            }
            if (mainView != null)
            {
                mainView.OnGameOver(false, isWin, score);
            }
            
            // 发送关卡结束消息
            SocketManager.Instance.SendLevelEnd(this.level, isWin, score);
            
            XLogger.Flush();
        }

        public void ContinueGame()
        {
            this.pause = false;
            player.hp = 100;
            player.point = 0;
            List<int> destroyList = new List<int>();
            //清除所有的agent
            foreach(var kv in entityDict)
            {
                BaseEntity entity = kv.Value;
                if(entity.Id!=player.Id)
                {
                    // if(Vector3.Distance(entity.GetPosition(), player.GetPosition())<player.HOOK_CATCH_RADIUS)
                    {
                        destroyList.Add(entity.Id);
                    }
                }
            }
            for(int x = 0; x<destroyList.Count; x++)
            {
                int eid = destroyList[x];
                BaseEntity entity = entityDict[eid];
                entity.Destroy();
                entityDict.Remove(eid);
            }
            //entityDict.Clear();
            //entityDict.Add(player.Id, player);
            //从当前关死亡的那一个wave开始生成agent，游戏时间也调整
            List<AgentConfig> agentConfigs = BaseConfig.GetLevelConfig(this.level);
            //Debug.LogError(" lastAgentIndex " + lastAgentIndex + ", count " + agentConfigs.Count);
            for (int i = lastAgentIndex; i>=0; i--)
            {
                //找到对应wave
                if(i>0 && agentConfigs[i].wave != agentConfigs[i-1].wave)
                {
                    this.lastAgentIndex = i - 1;
                    this.gameTime = agentConfigs[i].bornTime;
                    break;
                }
            }
            isGameOver = false;
        }

        public void RealExitGame()
        {
            //销毁游戏节点
            player.ExitGame();
            foreach (var kv in entityDict)
            {
                kv.Value.Destroy();
            }
            entityDict.Clear();
            Resources.UnloadUnusedAssets();
        }

        public void Pause()
        {
            this.pause = !this.pause;
        }
        public void ForceEnter(int level)
        {
            this.ExitGame();
            if(this.mainView)
            {
                this.mainView.StartGameByLv(level);
            }
        }
        public BaseEntity GetEntityByID(int id)
        {
            return entityDict.ContainsKey(id)?entityDict[id]:null;
        }

        public void HitPlayer(int entityId)
        {
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
            if (isGameOver) return;
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
                SessionLogger.Instance.MarkExited(entity.Id);
                bool dup = SessionLogger.Instance.IsDuplicateSpawn(entity.config != null ? entity.config.spawnId : "");
                SessionLogger.Instance.Enqueue(AgentEvent("block", entity, 0, 0, true, dup));
                XLogger.Info(string.Format(" shield-bashed the agent [{0}], pos={1}", entity.name, entityPos.ToString()));
                // entity.Destroy();
                // entityDict.Remove(entityId);
            }
        }

        public void ExitGame()
        {
            if (player != null && !player.isDestroy)
            {
                SessionLogger.Instance.EndLevel((int)player.hp, player.point, CollectAgentSnapshots());
            }
            isGameOver = true;
            this.RealExitGame();
        }


        public List<AgentSnapshot> CollectAgentSnapshots()
        {
            List<AgentSnapshot> list = new List<AgentSnapshot>();
            if (player == null || entityDict == null)
            {
                return list;
            }
            foreach (var kv in entityDict)
            {
                BaseEntity entity = kv.Value;
                if (entity == null || entity == player || entity.isDestroy || entity.config == null)
                {
                    continue;
                }
                if (SessionLogger.Instance.IsExited(entity.Id))
                {
                    continue;
                }
                string spawnId = entity.config.spawnId ?? "";
                list.Add(new AgentSnapshot
                {
                    EntityId = entity.Id,
                    SpawnId = spawnId,
                    AgentType = EntityUtils.GetLogAgentType(entity),
                    Wave = entity.config.wave,
                    Distance = Vector3.Distance(entity.GetPosition(), player.GetPosition()),
                    EnteredAt = entity.logEnteredAt,
                    DuplicateSpawnWarning = SessionLogger.Instance.IsDuplicateSpawn(spawnId)
                });
            }
            return list;
        }

        public static SessionLogEvent AgentEvent(string eventType, BaseEntity entity, int hpDelta, int goldDelta, bool inField, bool duplicateWarning)
        {
            float dist = float.NaN;
            Player p = CombatMgr.Instance().player;
            if (entity != null && p != null)
            {
                dist = Vector3.Distance(entity.GetPosition(), p.GetPosition());
            }
            return new SessionLogEvent
            {
                EventType = eventType,
                EntityId = entity != null ? entity.Id : 0,
                SpawnId = entity != null && entity.config != null ? (entity.config.spawnId ?? "") : "",
                AgentType = EntityUtils.GetLogAgentType(entity),
                Wave = entity != null && entity.config != null ? entity.config.wave : 0,
                HpDelta = hpDelta,
                GoldDelta = goldDelta,
                Distance = dist,
                InField = inField,
                DuplicateSpawnWarning = duplicateWarning
            };
        }
    }

}
