using UnityEngine;

namespace Miner.GameLogic
{
    public class Player
    {
        public PlayerController playCtrl;
        public GameObject go;
        public int Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Exp { get; set; }
        public int Gold { get; set; }
        public int Diamond { get; set; }
        public Player(GameObject go)
        {
            playCtrl = go.AddComponent<PlayerController>();
            this.go = go;
        }

        public void ExitGame()
        {
         
        }

        public void SetPosition(Vector3 pos)
        {
            go.transform.position = pos;
        }
    }
}