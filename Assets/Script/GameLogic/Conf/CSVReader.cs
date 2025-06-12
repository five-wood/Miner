using System.Collections.Generic;
using System.IO;
#if UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
#endif
namespace Miner.GameLogic
{
    public class CSVReader
    {
#if UNITY_STANDALONE_WIN
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int MessageBox(int hWnd, string text, string caption, uint type);
#endif
        public static void ShowNativePopup(string message, string title = "提示")
        {
#if UNITY_STANDALONE_WIN
            MessageBox(0, message, title, 0);
#endif
        }

        public static List<Dictionary<string, string>> ReadCSV(string filePath)
        {
            List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
            if(!File.Exists(filePath))
            {
                ShowNativePopup("配置文件不存在：" + filePath, "异常");
            }
            // 读取CSV文件
            using (StreamReader reader = new StreamReader(filePath))
            {
                // 读取标题行
                string[] headers = reader.ReadLine().Split(',');

                while (!reader.EndOfStream)
                {
                    string[] values = reader.ReadLine().Split(',');
                    Dictionary<string, string> entry = new Dictionary<string, string>();

                    for (int i = 0; i < headers.Length; i++)
                    {
                        entry[headers[i]] = values[i];
                    }

                    data.Add(entry);
                }
            }
            return data;
        }

    }
}