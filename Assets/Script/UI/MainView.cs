using UnityEngine;
using UnityEngine.UI;
using Miner.GameLogic;

namespace Miner.UI
{
    public class MainView: MonoBehaviour
    {
        public Button StartButton;
        public Button ExitButton;

        public Slider HpSlider;

        public GameObject beforeGo;
        public InputField levelInputField;
        public GameObject gamingGo;
        public Text pointText;

        public GameObject afterGo;
        public Text resultTxt;
        public Button againButton;


        private int _level = 1;

        public void Start()
        {
            StartButton.onClick.AddListener(StartGame);
            ExitButton.onClick.AddListener(ExitGame);
            levelInputField.onValueChanged.AddListener(OnLevelInputChanged);
            againButton.onClick.AddListener(OnAgainButtonClick);
            beforeGo.SetActive(true);
            gamingGo.SetActive(false);
            afterGo.SetActive(false);
        }

        private void StartGame()
        {
            CombatMgr.Instance().LoadGame(this._level);
            beforeGo.SetActive(false);
            gamingGo.SetActive(true);
            afterGo.SetActive(false);
        }   

        public void ExitGame()
        {
            CombatMgr.Instance().OnGameOver();
            OnGameOver(true);
        }

        public void OnGameOver(bool force, bool isWin = false)
        {
            if(force)
            {
                beforeGo.SetActive(true);
                gamingGo.SetActive(false);
                afterGo.SetActive(false);
            }
            else
            {
                beforeGo.SetActive(false);
                gamingGo.SetActive(false);
                afterGo.SetActive(true);
                resultTxt.text = isWin?"You Win":"Game Over";
            }
        }

        private void Destroy()
        {
            StartButton.onClick.RemoveListener(StartGame);
            ExitButton.onClick.RemoveListener(ExitGame);
        }

        private void OnLevelInputChanged(string levelInput)
        {
            int level = int.Parse(levelInput);
            _level = level>0?level:1;
        }

        private void OnAgainButtonClick()
        {
            beforeGo.SetActive(true);
            gamingGo.SetActive(false);
            afterGo.SetActive(false);
        }
    }
}