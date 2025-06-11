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

        public Text hpChangeTxt;
        public Text pointChangeTxt;
        private Animator hpAnim;
        private Animator pointAnim;



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
            hpAnim = hpChangeTxt.gameObject.GetComponent<Animator>();
            pointAnim = pointChangeTxt.gameObject.GetComponent<Animator>();
        }

        private void StartGame()
        {
            CombatMgr.Instance().LoadGame(this._level);
            beforeGo.SetActive(false);
            gamingGo.SetActive(true);
            afterGo.SetActive(false);
            hpChangeTxt.text = "";
            pointChangeTxt.text = "";
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

        public void ChangeHp(float value)
        {
            if (Mathf.Approximately(value, 0)) return;
            string str = "<color=\"#ee0000\">-{0}</color>";
            if(value > 0)
            {
                str = "<color=\"#00ee00\">+{0}</color>";
            }
            pointChangeTxt.enabled = true;
            hpChangeTxt.text = string.Format(str, (int)(Mathf.Abs(value)));
            hpAnim.Play("hpJump",0,0f);
        }

        public void ChangePoint(int value)
        {
            if (value == 0)
                return;
            pointChangeTxt.enabled = true;
            if(value>0)
            {
                pointChangeTxt.text = string.Format("<color=\"#00ee00\">+{0}</color>", value);
            }
            else
            {
                pointChangeTxt.text = string.Format("<color=\"#ee0000\">{0}</color>", value);
            }
            pointAnim.Play("pointJump",0,0f);
        }
    }
}