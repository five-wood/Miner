using UnityEngine;
using UnityEngine.UI;
using Miner.GameLogic;
using System;

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
        public GameObject finalGo;

        public GameObject afterGo;
        public Text resultTxt;
        public Button nextBtn;
        public Button exitButton;


        public Text hpChangeTxt;
        public Text pointChangeTxt;
        private Animator hpAnim;
        private Animator pointAnim;

        public GameObject winGo;
        public Text winScoreTxt;
        public GameObject loseGo;
        public Text loseTimerTxt;
        public Text loseScoreTxt;
        private float loseTimer = 0;
        private float winTimer = 0;

        private int _level = 1;

        public void Start()
        {
            StartButton.onClick.AddListener(OnStartGame);
            ExitButton.onClick.AddListener(OnExitGame);
            levelInputField.onValueChanged.AddListener(OnLevelInputChanged);
            nextBtn.onClick.AddListener(OnNextBtnClick);
            exitButton.onClick.AddListener(OnExitButtonClick);
            beforeGo.SetActive(true);
            gamingGo.SetActive(false);
            afterGo.SetActive(false);
            finalGo.SetActive(false);
            hpAnim = hpChangeTxt.gameObject.GetComponent<Animator>();
            pointAnim = pointChangeTxt.gameObject.GetComponent<Animator>();
            hpAnim.enabled = false;
            pointAnim.enabled = false;
            pointChangeTxt.transform.GetChild(0).gameObject.SetActive(false);
        }

        public bool startRecord = false;
        public float recordDuration = 0;
        private void OnStartGame()
        {
            startRecord = true;
            recordDuration = 0;
            StartGameByLv(1);
        }   

        public void StartGameByLv(int lv)
        {
            if (lv > BaseConfig.maxLevel)
            {
                return;
            }
            XLogger.Info(string.Format("========Start Game Run:{0}==========", lv));
            this._level = lv;
            CombatMgr.Instance().LoadGame(this._level);
            beforeGo.SetActive(false);
            gamingGo.SetActive(true);
            afterGo.SetActive(false);
            finalGo.SetActive(false);
            hpChangeTxt.text = "";
            pointChangeTxt.text = "";
        }

        public void OnExitGame()
        {
            CombatMgr.Instance().OnGameOver();
            OnGameOver(true);
        }

        public void OnGameOver(bool force, bool isWin = false, int point = 0)
        {
            if(force)
            {
                beforeGo.SetActive(true);
                gamingGo.SetActive(false);
                afterGo.SetActive(false);
                finalGo.SetActive(false);
            }
            else
            {
                beforeGo.SetActive(false);
                gamingGo.SetActive(false);
                afterGo.SetActive(true);
                finalGo.SetActive(false);
                resultTxt.text = isWin?"You Win":"Game Over";
                winGo.SetActive(isWin);
                loseGo.SetActive(!isWin);
                winTimer = 0;loseTimer = 0;
                if (isWin)
                {
                    winScoreTxt.text = string.Format("{0}", point);
                    this.nextBtn.gameObject.SetActive(false);
                    winTimer = 60;
                }
                else
                {
                    loseScoreTxt.text = point.ToString();
                    loseTimer = 5;
                    loseTimerTxt.text = string.Format("{0} seconds", (int)(loseTimer+1));
                }
            }
        }

        private void Destroy()
        {
            StartButton.onClick.RemoveListener(OnStartGame);
            ExitButton.onClick.RemoveListener(OnExitGame);
        }

        private void OnLevelInputChanged(string levelInput)
        {
            int level = int.Parse(levelInput);
            _level = level>0?level:1;
        }

        private void OnNextBtnClick()
        {
            CombatMgr.Instance().RealExitGame();
            if(this._level == BaseConfig.maxLevel)
            {
                beforeGo.SetActive(false);
                gamingGo.SetActive(false);
                afterGo.SetActive(false);
                finalGo.SetActive(true);
            }
            else
            {
                this.StartGameByLv(this._level + 1);
            }
        }

        private void OnExitButtonClick()
        {
            beforeGo.SetActive(true);
            gamingGo.SetActive(false);
            afterGo.SetActive(false);
            finalGo.SetActive(false);
            CombatMgr.Instance().RealExitGame();
        }

        private float hpChanged = 0;
        private float hpJumpDelay =0;
        public void ChangeHp(float value)
        {
            if (Mathf.Approximately(value, 0)) return;
            hpChanged += value;
            hpJumpDelay = Math.Max(0.1f, Mathf.Min(hpJumpDelay + 0.2f, 1.2f));
        }

        public void JumpHp(float value)
        {
            string str = "<color=\"#ee0000\">-{0}</color>";
            if (value > 0)
            {
                str = "<color=\"#00ee00\">+{0}</color>";
            }
            hpChangeTxt.enabled = true;
            hpChangeTxt.text = string.Format(str, (int)(Mathf.Abs(value)));
            hpAnim.enabled = true;
            hpAnim.Play("hpJump", 0, 0f);
            hpChanged = 0;
        }

        private int pointChanged = 0;
        private float pointJumpDelay = 0;
        public void ChangePoint(int curValue)
        {
            if (curValue == 0)
                return;
            pointChanged += curValue;
            pointJumpDelay = Math.Max(0.1f, Mathf.Min(pointJumpDelay + 0.2f, 1.2f));
        }
        private void JumpPoint(int curValue)
        {
            pointChangeTxt.enabled = true;
            if (curValue > 0)
            {
                pointChangeTxt.text = string.Format("<color=\"#00ee00\">+{0}</color>", curValue);
            }
            else
            {
                pointChangeTxt.text = string.Format("<color=\"#ee0000\">{0}</color>", curValue);
            }
            pointAnim.enabled = true;
            pointAnim.Play("pointJump", 0, 0f);
            pointChanged = 0;
        }

        public void Update()
        {
            if (startRecord)
            {
                recordDuration += Time.deltaTime;
                if(recordDuration>1)
                {
                    CombatMgr.Instance().Record();
                    recordDuration = 0;
                }
            }

            if (loseTimer>0)
            {
                loseTimer -= Time.deltaTime;
                loseTimerTxt.text = string.Format("{0} seconds", (int)(loseTimer + 1));
                if(loseTimer<=0) //倒计时结束，回到上一关继续玩
                {
                    beforeGo.SetActive(false);
                    gamingGo.SetActive(true);
                    afterGo.SetActive(false);
                    finalGo.SetActive(false);
                    CombatMgr.Instance().ContinueGame();
                }
            }
            if(winTimer>0)
            {
                winTimer -= Time.deltaTime;
                if(winTimer<=0)
                {
                    this.nextBtn.gameObject.SetActive(true);
                }
            }
            
            if(hpJumpDelay>0)
            {
                hpJumpDelay -= Time.deltaTime;
                if(hpJumpDelay<0)
                {
                    JumpHp(this.hpChanged);
                }
            }

            if(pointJumpDelay>0)
            {
                pointJumpDelay -= Time.deltaTime;
                if(pointJumpDelay<0)
                {
                    JumpPoint(this.pointChanged);
                }
            }
        }
    }
}