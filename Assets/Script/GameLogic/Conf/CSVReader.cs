using System.Collections.Generic;
using System.IO;

namespace Miner.GameLogic
{
    public class CSVReader
    {
         public static List<Dictionary<string, string>> ReadCSV(string filePath)
        {
            List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
            
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