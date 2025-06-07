using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace Miner.GameLogic
{
    public class BulletComp:MonoBehaviour
    {
        float speed = 50;
        List<GameObject> bulletGoList = new List<GameObject>();
        List<Vector3> targetPosList = new List<Vector3>();

        public void Shot(Vector3 targetPos, Vector3 ownerPos)
        {
            GameObject bulletPrefab = Resources.Load<GameObject>(ResConst.bulletPath);
            GameObject go = GameObject.Instantiate(bulletPrefab, ownerPos, Quaternion.identity);
            bulletGoList.Add(go);
            targetPosList.Add(targetPos);
        }

        void LateUpdate()
        {
            for(int i=bulletGoList.Count-1; i>=0; i--)
            {
                GameObject bulletGo = bulletGoList[i];
                Vector3 targetPos = targetPosList[i];
                bulletGo.transform.position = Vector3.MoveTowards(bulletGo.transform.position, targetPos, speed * Time.deltaTime);
                if(Vector3.Distance(bulletGo.transform.position, targetPos) < 0.1f)
                {
                    RemoveBullet(i);
                }
            }
        }

        public void RemoveBullet(int index)
        {
            if(index < 0 || index >= bulletGoList.Count)
            {
                return;
            }
            GameObject bulletGo = bulletGoList[index];
            bulletGoList.RemoveAt(index);
            GameObject.Destroy(bulletGo);
            targetPosList.RemoveAt(index);
        }

        public void Destroy()
        {
            foreach(GameObject bulletGo in bulletGoList)
            {
                GameObject.Destroy(bulletGo);
            }
            bulletGoList.Clear();
            targetPosList.Clear();
        }
    }
}