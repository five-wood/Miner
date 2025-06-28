using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Miner.GameLogic
{
    public class XLogger
    {
        private static StreamWriter writer;
        public static void OpenLogFile(string filePath)
        {
            if (writer!=null) return;
            writer = new StreamWriter(filePath);
        }

        public static void Info(string msg)
        {
            if (writer == null) return;
            writer.WriteLine(string.Format("{2}-{0} {1}", System.DateTime.Now.ToLongTimeString(), msg, System.DateTime.Now.ToLongDateString()));
        }

        public static void Flush()
        {
            if (writer == null) return;
            writer.Flush();
        }

        public static void Stop()
        {
            Debug.Log("XLogger Stop");
            Flush();
            writer.Dispose();
        }
    }
}

