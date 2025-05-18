using UnityEngine;

namespace Miner.GameLogic
{
    public class BaseEntity
    {
        public BaseEntity()
        {
            LoadPrefab();
        }
        public GameObject go;
        public int Id { 
            get{
                if(go == null)
                {
                    return 0;
                }
                return go.GetComponent<IDComp>().id;
            }
        }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Exp { get; set; }
        public int Gold { get; set; }
        public int Diamond { get; set; }

        public virtual string GetPrefabPath()
        {
            return "";
        }

        public void LoadPrefab()
        {
            string path =  this.GetPrefabPath();
            if(!string.IsNullOrEmpty(path))
            {
                GameObject perfab = Resources.Load<GameObject>(path);
                go = GameObject.Instantiate(perfab);
                go.transform.SetParent(CombatMgr.Instance().sceneGo.transform);
                go.AddComponent<IDComp>();
                go.GetComponent<IDComp>().SetID(IDComp.IDcount++);
            }
            else
            {
                Debug.LogError("GetPrefabPath is null");
            }
        }

        
        public void SetPosition(Vector3 pos)
        {
            go.transform.position = pos;
        }

        public Vector3 GetPosition()
        {
            return go.transform.position;
        }


        public void Destroy()
        {
            GameObject.Destroy(go);
        }
        public virtual void Update(float deltaTime){}
    }
}