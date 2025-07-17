using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Miner.GameLogic
{
    public class XLogger
    {
        private static StreamWriter writer;
        private static int second = 0;
   
        //废弃接口
        public static void Info(string msg)
        {
            return;
            if(writer == null) return;
            writer.WriteLine(string.Format("{2}-{0} {1}", System.DateTime.Now.ToLongTimeString(), msg, System.DateTime.Now.ToLongDateString()));
        }

        public static void Record(string msg)
        {
            second++;
            if (writer == null) return;
            writer.WriteLine(string.Format("\"{0}-{1}\",{2},{3}", System.DateTime.Now.ToShortDateString(), System.DateTime.Now.ToLongTimeString(), second, msg ));
        }

        public static void Flush()
        {
            if (writer == null) return;
            writer.Flush();
        }

        public static void Stop()
        {
            //Debug.Log("XLogger Stop");
            Flush();
            writer.Dispose();
            writer = null;
        }

        public static void CreateCSV(string path)
        {
            if(File.Exists(path))
            {
                File.Delete(path);
            }
            writer = new StreamWriter(path);
            writer.WriteLine("Date,Second,Level,Wave,Avatar_facing,Avatar_action,Avatar_action_face,Avatar_action_detail,Avatar_health,Avatar_healthchange,Avatar_gold,Avatar_goldchange,Agent_number,Positive_contact,Negative_contact,Reward_number,Reward_nearest_distance,Reward_nearest_location_x,Reward_nearest_location_y,Reward_inrange_count,Reward_outrange_count,Reward_entry_direction,Reward_entry_angle,Threat_number,Threat_nearest_distance,Threat_nearest_location_x,Threat_nearest_location_y,Threat_projectile_hit,Threat_projectile_count,Threat_inrange_count,Threat_outrange_count,Threat_entry_direction,Threat_entry_angle,Coactive1_number,Coactive1_nearest_distance,Coactive1_nearest_location_x,Coactive1_nearest_location_y,Coactive1_inrange_count,Coactive1_outrange_count,Coactive1_entry_direction,Coactive1_entry_angle,Coactive2_number,Coactive2_nearest_distance,Coactive2_nearest_location_x,Coactive2_nearest_location_y,Coactive2_inrange_count,Coactive2_outrange_count,Coactive2_entry_direction,Coactive2_entry_angle,Action_event_start,Action_event_end");
            writer.Flush();
        }
    }
}

