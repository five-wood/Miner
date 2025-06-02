namespace Miner.GameLogic
{
    public class BaseConfig
    {
        public int level;

        public float roundTime = 6; //3秒一波怪

        public float moveSpeed = 5f;

        public virtual void InitConfig(){}
    }
}