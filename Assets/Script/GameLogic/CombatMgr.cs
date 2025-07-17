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
            if(player.hp > 0)
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
                mainView.OnGameOver(false, player.hp > 0, player.point);
            }
            XLogger.Flush();
        }

        public void ContinueGame()
        {
            this.pause = false;
            player.hp = 100;
            player.point = 0;
            List<int> destroyList = new List<int>();
            //清除所有在圈内的agent
            foreach(var kv in entityDict)
            {
                BaseEntity entity = kv.Value;
                if(entity.Id!=player.Id)
                {
                    if(Vector3.Distance(entity.GetPosition(), player.GetPosition())<player.HOOK_CATCH_RADIUS)
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
                XLogger.Info(string.Format(" shield-bashed the agent [{0}], pos={1}", entity.name, entityPos.ToString()));
                // entity.Destroy();
                // entityDict.Remove(entityId);
            }
        }

        public void ExitGame()
        {
            isGameOver = true;
            this.RealExitGame();
        }


        public void Record()
        {
            if (player == null || player.isDestroy)
                return;
            string Level = isGameOver? "-" : this.level.ToString();
            string Wave = isGameOver ? "-" : this.wave.ToString();
            string Avatar_facing = RecordUtils.GetPlayerFace(player);
            string Avatar_action = RecordUtils.GetCurActions();
            string Avatar_action_face = RecordUtils.GetActionFaces();
            string Avatar_action_detail = RecordUtils.GetCurActionDetails();
            float Avatar_health = player == null ? 0 : player.hp;
            string Avatar_healthchange = RecordUtils.GetHpChanged();
            int Avatar_gold = player == null ? 0 : player.point;
            string Avatar_goldchange = RecordUtils.GetGoldChanged();

            //当前屏幕上所有 agent 的总数
            int Agent_number = entityDict == null ? 0 : entityDict.Count - 1;
            string positive_contact = RecordUtils.GetPositiveHit();
            string negative_contact = RecordUtils.GetNegativeHit();

            //reward 的总数量
            BaseEntity nearestReward = NearestAgent("Lucky grass", out int rewardInCnt, out int rewardOutCnt, out float rewardNearestDis);
            int Reward_number = (rewardInCnt + rewardOutCnt);
            float Reward_nearest_distance = rewardNearestDis;
            float Reward_nearest_location_x = nearestReward!=null?nearestReward.GetPosition().x:0;
            float Reward_nearest_location_y = nearestReward!=null?nearestReward.GetPosition().y:0;
            int Reward_inrange_count = rewardInCnt;
            int Reward_outrange_count = rewardOutCnt;
            string Reward_entry_direction = RecordUtils.GetAngle(nearestReward).ToString();
            string Reward_entry_angle = nearestReward!=null? Enum.GetName(typeof(PositionEnum), nearestReward.config.posType):"none";

            //Threat
            BaseEntity nearestThreat = NearestAgent("Toxic Vine", out int threateInCnt, out int threatOutCnt, out float threatNearestDis);
            int Threat_number = threateInCnt + threatOutCnt;
            float Threat_nearest_distance = threatNearestDis;
            float Threat_nearest_location_x = nearestThreat!=null?nearestThreat.GetPosition().x:0;
            float Threat_nearest_location_y = nearestThreat!=null?nearestThreat.GetPosition().y:0;
            int Threat_projectile_hit = RecordUtils.isPlayHurt;
            int Threat_projectile_count = RecordUtils.threatShootNum;
            int Threat_inrange_count = threateInCnt;
            int Threat_outrange_count = threatOutCnt;
            string Threat_entry_direction = RecordUtils.GetAngle(nearestThreat).ToString();
            string Threat_entry_angle = nearestThreat!=null?Enum.GetName(typeof(PositionEnum),nearestThreat.config.posType):"none";

            //Tall Mushrooms
            BaseEntity nearestTallMushroom = NearestAgent("Tall Mushroom", out int tallInCnt, out int tallOutCnt, out float tallNearestDis);
            int Coactive1_number = tallOutCnt + tallInCnt;
            float Coactive1_nearest_distance = tallNearestDis;
            float Coactive1_nearest_location_x = nearestTallMushroom!=null?nearestTallMushroom.GetPosition().x:0;
            float Coactive1_nearest_location_y = nearestTallMushroom!=null?nearestTallMushroom.GetPosition().y:0;
            int Coactive1_inrange_count = tallInCnt;
            int Coactive1_outrange_count = tallOutCnt;
            string Coactive1_entry_direction = RecordUtils.GetAngle(nearestTallMushroom).ToString();
            string Coactive1_entry_angle = nearestTallMushroom!=null? Enum.GetName(typeof(PositionEnum), nearestTallMushroom.config.posType):"none";

            BaseEntity nearestFatMushroom = NearestAgent("Fat Mushroom", out int fatInCnt, out int fatOutCnt, out float fatNearestDis);
            int Coactive2_number = fatInCnt + fatOutCnt;
            float Coactive2_nearest_distance = fatNearestDis;
            float Coactive2_nearest_location_x = nearestFatMushroom!=null?nearestFatMushroom.GetPosition().x:0;
            float Coactive2_nearest_location_y = nearestFatMushroom!=null?nearestFatMushroom.GetPosition().y:0;
            int Coactive2_inrange_count = fatInCnt;
            int Coactive2_outrange_count = fatOutCnt;
            string Coactive2_entry_direction = RecordUtils.GetAngle(nearestFatMushroom).ToString();
            string Coactive2_entry_angle = nearestFatMushroom!=null? Enum.GetName(typeof(PositionEnum), nearestFatMushroom.config.posType):"none";

            string Action_event_start = RecordUtils.GetActionStartTimes();
            string Action_event_end = RecordUtils.GetActionEndTimes();
            string msg = string.Join(",",
                Level,
                Wave,
                Avatar_facing,
                Avatar_action,
                Avatar_action_face,
                Avatar_action_detail,
                Avatar_health,
                Avatar_healthchange,
                Avatar_gold,
                Avatar_goldchange,
                Agent_number.ToString(),
                positive_contact,
                negative_contact,
                Reward_number.ToString(),
                Reward_nearest_distance.ToString(),
                Reward_nearest_location_x.ToString(),
                Reward_nearest_location_y.ToString(),
                Reward_inrange_count.ToString(),
                Reward_outrange_count.ToString(),
                Reward_entry_direction,
                Reward_entry_angle.ToString(),
                Threat_number.ToString(),
                Threat_nearest_distance.ToString(),
                Threat_nearest_location_x.ToString(),
                Threat_nearest_location_y.ToString(),
                Threat_projectile_hit.ToString(),
                Threat_projectile_count.ToString(),
                Threat_inrange_count.ToString(),
                Threat_outrange_count.ToString(),
                Threat_entry_direction,
                Threat_entry_angle.ToString(),
                Coactive1_number.ToString(),
                Coactive1_nearest_distance.ToString(),
                Coactive1_nearest_location_x.ToString(),
                Coactive1_nearest_location_y.ToString(),
                Coactive1_inrange_count.ToString(),
                Coactive1_outrange_count.ToString(),
                Coactive1_entry_direction,
                Coactive1_entry_angle.ToString(),
                Coactive2_number.ToString(),
                Coactive2_nearest_distance.ToString(),
                Coactive2_nearest_location_x.ToString(),
                Coactive2_nearest_location_y.ToString(),
                Coactive2_inrange_count.ToString(),
                Coactive2_outrange_count.ToString(),
                Coactive2_entry_direction,
                Coactive2_entry_angle.ToString(),
                Action_event_start,
                Action_event_end
            );
            XLogger.Record(msg);

            RecordUtils.Clear();
        }

        private BaseEntity NearestAgent(string agentName, out int inRangeCnt, out int outRangeCnt, out float nearestDis )
        {
            inRangeCnt = 0;
            outRangeCnt = 0;
            nearestDis = 999999;
            if (isGameOver)
            {
                return null;
            }
            BaseEntity nearest = null;
            foreach (var kv in entityDict)
            {
                BaseEntity entity = kv.Value;
                if(entity.name == agentName)
                {
                    float distance = Vector3.Distance(entity.GetPosition(), player.GetPosition());
                    if(distance<player.HOOK_CATCH_RADIUS)
                        inRangeCnt++;
                    else
                        outRangeCnt++;
                    if(distance< nearestDis)
                    {
                        nearest = entity;
                        nearestDis = distance;
                    }
                }
            }
            return nearest;
        }
    }

}