using UnityEngine;
namespace Miner.GameLogic
{
    public class IDComp:MonoBehaviour
    {
        public static int IDcount = 0;
        public int id;

        public void SetID(int id)
        {
            this.id = id;
        }
    }
}