using System;
using System.Collections.Generic;

namespace Miner.GameLogic
{
    enum ActionType
    {
        none,
        hook, //出钩
        block,  //出盾
    }

    enum ActionResult
    {
        SuccHook,
        MissingHook,
        SuccBlock,
        MissingBlock,
    }

    class RecordUtils
    {
        static public string GetPlayerFace(Player player)
        {
            if (player == null) return "";
            float angle = player.arrowTrans.transform.localEulerAngles.z % 360;
            if(0<=angle && angle<90)
            {
                return "up-left"; 
            }
            else if(90<=angle && angle<180)
            {
                return "down-right";
            }
            else if(180<=angle && angle<270)
            {
                return "down-right";
            }
            else if(270<=angle && angle<360)
            {
                return "up-right";
            }
            return "";
        }

        static public int GetAngle(BaseEntity entity)
        {
            if (entity == null) return -1;
            PositionEnum pos = entity.config.posType;
            switch (pos)
            {
                case PositionEnum.TopLeft:
                    return 45;
                case PositionEnum.TopRight:
                    return 315;
                case PositionEnum.BottomLeft:
                    return 135;
                case PositionEnum.BottomRight:
                    return 225;
            }
            return -1;
        }

        static public List<ActionType> actionTypes = new List<ActionType>();
        static public List<float> hpChanged = new List<float>();
        static public List<ActionResult> actionDetailTypes = new List<ActionResult>();
        static public List<string> positiveHit = new List<string>();
        static public List<string> negativeHit = new List<string>();
        static public int isPlayHurt = 0;
        static public int threatShootNum = 0;
        static public List<string> actionStartTimes = new List<string>();
        static public List<string> actionEndTimes = new List<string>();
        static public List<string> actionFaces = new List<string>();

        static public string GetCurActions()
        {
            string log = "";
            actionTypes.ForEach((value) =>
            {
                log += Enum.GetName(typeof(ActionType), value) + ",";
            });
            return string.Format("\"{0}\"", log);
        }

        static public string GetCurActionDetails()
        {
            string log = "";
            actionDetailTypes.ForEach((value) =>
            {
                log += Enum.GetName(typeof(ActionResult), value) + ",";
            });
            return string.Format("\"{0}\"", log);
        }

        static public string GetHpChanged()
        {
            string log = "";
            hpChanged.ForEach((value) =>
            {
                log += value + ",";
            });
            return string.Format("\"{0}\"",log);
        }

        static public string GetPositiveHit()
        {
            string log = "";
            positiveHit.ForEach((value) =>
            {
                log += value + ",";
            });
            return string.Format("\"{0}\"", log);
        }

        static public string GetNegativeHit()
        {
            string log = "";
            negativeHit.ForEach((value) =>
            {
                log += value + ",";
            });
            return string.Format("\"{0}\"", log);
        }

        static public string GetActionStartTimes()
        {
            string log = "";
            actionStartTimes.ForEach((value) =>
            {
                log += value + ",";
            });
            return string.Format("\"{0}\"", log);
        }
        static public string GetActionEndTimes()
        {
            string log = "";
            actionEndTimes.ForEach((value) =>
            {
                log += value + ",";
            });
            return string.Format("\"{0}\"", log);
        }

        static public string GetActionFaces()
        {
            string log = "";
            actionFaces.ForEach((value) =>
            {
                log += value + ",";
            });
            return string.Format("\"{0}\"", log);
        }


        static public void Clear()
        {
            actionTypes.Clear();
            actionDetailTypes.Clear();
            hpChanged.Clear();
            positiveHit.Clear();
            negativeHit.Clear();
            isPlayHurt = 0;
            threatShootNum = 0;
            actionStartTimes.Clear();
            actionEndTimes.Clear();
            actionFaces.Clear();
        }
    }
}
