using UnityEngine;
using UnityEngine.UI;
using Miner.GameLogic;

namespace Miner.UI
{
    public class MainView: MonoBehaviour
    {
        public Button StartButton;
        public Button ExitButton;

        public void Start()
        {
            StartButton.onClick.AddListener(StartGame);
            ExitButton.onClick.AddListener(ExitGame);
        }

        private void StartGame()
        {
            CombatMgr.Instance().LoadGame(1);
        }   

        private void ExitGame()
        {
            CombatMgr.Instance().ExitGame();
        }

        private void Destroy()
        {
            StartButton.onClick.RemoveListener(StartGame);
            ExitButton.onClick.RemoveListener(ExitGame);
        }
        
    }
}