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
        private bool deathWaiting = false;
        private float deathWaitRemaining = 0f;
        private int deathGold = 0;
        private const float DeathWaitDuration = 5f;

        public bool IsDeathWaiting { get { return deathWaiting; } }
        public float DeathWaitRemaining { get { return deathWaitRemaining; } }

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
            this.deathWaiting = false;
            this.deathWaitRemaining = 0f;
            this.deathGold = 0;
            this.wave = 0;
            CreatePlayer();
            SessionLogger.Instance.StartLevel(this.level);
        }

        private List<int> _destroyList = new List<int>();
        public void UpdateGame(float deltaTime)
        {
            if (isGameOver || pause)
            {
                return;
            }

            if (deathWaiting)
            {
                float waitStep = GetDeathWaitStep(deathWaitRemaining, deltaTime);
                this.gameTime += waitStep;
                deathWaitRemaining -= waitStep;
                SessionLogger.Instance.TickDeathWait(waitStep, deathGold);
                if (deathWaitRemaining <= 0f)
                {
                    CompleteDeathWait();
                }
                return;
            }
            this.gameTime += deltaTime;
            bool hadNextAgent = this.CheckRound();
            UpdateEntities(deltaTime);

            if (player != null && !player.isDestroy)
            {
                SessionLogger.Instance.Tick(deltaTime, (int)player.hp, player.point, CollectAgentSnapshots());
            }
            UpdateMainView();

            if (player.hp <= 0)
            {
                BeginDeathWait();
            }
            else if (!hadNextAgent && entityDict.Count == 1)
            {
                FinishLevel(true);
            }
        }

        private void UpdateEntities(float deltaTime)
        {
            _destroyList.Clear();
            foreach (var entityKV in entityDict)
            {
                BaseEntity entity = entityKV.Value;
                entity.Update(deltaTime);
                if (entity.delayDestoryTime > 0)
                {
                    entity.delayDestoryTime -= deltaTime;
                    if (entity.delayDestoryTime <= 0)
                    {
                        entity.delayDestoryTime = 0;
                        _destroyList.Add(entityKV.Key);
                    }
                }
            }
            for (int i = 0; i < _destroyList.Count; i++)
            {
                if (entityDict.ContainsKey(_destroyList[i]))
                {
                    entityDict[_destroyList[i]].Destroy();
                    entityDict.Remove(_destroyList[i]);
                }
            }
        }

        private void UpdateMainView()
        {
            if (mainView == null || player == null)
            {
                return;
            }
            mainView.HpSlider.value = Math.Max(0, player.hp) / 100.0f;
            mainView.pointText.text = string.Format("{0}", player.point);
        }

        public static float GetDeathWaitStep(float remaining, float deltaTime)
        {
            return Math.Min(Math.Max(remaining, 0f), Math.Max(deltaTime, 0f));
        }

        public static bool HasFutureConfig(List<AgentConfig> agentConfigs, int lastIndex, float currentTime)
        {
            return lastIndex < agentConfigs.Count - 1
                && agentConfigs[lastIndex + 1].totalTime > currentTime;
        }

        private void BeginDeathWait()
        {
            deathGold = player.point;
            SessionLogger.Instance.FlushPendingEvents(0, deathGold, new List<AgentSnapshot>());
            ClearAgents();
            if (!HasFutureConfig(BaseConfig.GetLevelConfig(level), lastAgentIndex, gameTime))
            {
                FinishLevel(false);
                return;
            }
            deathWaiting = true;
            if (mainView != null)
            {
                mainView.OnGameOver(false, false, deathGold);
            }
            deathWaitRemaining = DeathWaitDuration;
        }

        private void CompleteDeathWait()
        {
            List<AgentConfig> configs = BaseConfig.GetLevelConfig(level);
            lastAgentIndex = FindLastDueConfigIndex(configs, lastAgentIndex, gameTime);
            player.hp = 100;
            player.point = 0;
            deathWaiting = false;
            deathWaitRemaining = 0f;
            if (!HasFutureConfig(configs, lastAgentIndex, gameTime))
            {
                FinishLevel(false);
                return;
            }
            if (mainView != null)
            {
                mainView.ShowGameplayAfterDeathWait();
            }
        }

        private void ClearAgents()
        {
            List<int> destroyList = new List<int>();
            foreach (var kv in entityDict)
            {
                if (kv.Value != player)
                {
                    destroyList.Add(kv.Key);
                }
            }
            for (int i = 0; i < destroyList.Count; i++)
            {
                BaseEntity entity = entityDict[destroyList[i]];
                entity.Destroy();
                entityDict.Remove(destroyList[i]);
            }
        }

        private void FinishLevel(bool isWin)
        {
            isGameOver = true;
            int score = player != null ? player.point : 0;
            if (isWin)
            {
                SessionLogger.Instance.EndLevel((int)player.hp, score, CollectAgentSnapshots());
            }
            else
            {
                SessionLogger.Instance.EndLevel(0, score, new List<AgentSnapshot>());
            }
            SocketManager.Instance.SendLevelEnd(level, isWin, score);
            XLogger.Flush();
            if (mainView != null)
            {
                if (isWin)
                {
                    mainView.OnGameOver(false, true, score);
                }
                else
                {
                    mainView.HandleTerminalFailure(level, score);
                }
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

        public static int FindLastDueConfigIndex(List<AgentConfig> agentConfigs, int lastIndex, float currentTime)
        {
            int index = lastIndex;
            while (index < agentConfigs.Count - 1 && agentConfigs[index + 1].totalTime <= currentTime)
            {
                index++;
            }
            return index;
        }

        public bool CheckRound()
        {
            List<AgentConfig> agentConfigs = BaseConfig.GetLevelConfig(this.level);
            int dueThrough = FindLastDueConfigIndex(agentConfigs, this.lastAgentIndex, this.gameTime);
            for (int i = this.lastAgentIndex + 1; i <= dueThrough; i++)
            {
                AgentConfig agentConfig = agentConfigs[i];
                XLogger.Info("Create Agent ["+agentConfig.agentName+"],gameTime ="+ this.gameTime +"s");
                CreateItem(agentConfig);
                this.wave = agentConfig.wave;
            }
            this.lastAgentIndex = dueThrough;
            return this.lastAgentIndex < agentConfigs.Count - 1;
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
            else
            {
                SessionLogger.Instance.FlushPendingEvents((int)player.hp, player.point, CollectAgentSnapshots());
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


        public void RealExitGame()
        {
            if (player != null && !player.isDestroy)
            {
                SessionLogger.Instance.EndLevel((int)player.hp, player.point, CollectAgentSnapshots());
            }
            //销毁游戏节点
            if (player != null && !player.isDestroy)
            {
                player.ExitGame();
            }
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
