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
        }

        public static void Flush()
        {
            if (writer == null) return;
            writer.Flush();
        }

        public static void Stop()
        {
            if (writer == null) return;
            Flush();
            writer.Dispose();
            writer = null;
        }

        public static void CreateCSV(string path)
        {
        }
    }
}

