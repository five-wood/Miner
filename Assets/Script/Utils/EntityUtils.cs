using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Miner.GameLogic
{
    public class EntityUtils 
    {
        public static Collider CreateCollider(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            Rigidbody rigid = go.GetComponent<Rigidbody>(); 
            if (collider == null)
            {
                collider = go.AddComponent<BoxCollider>();
            }
            if(rigid == null)
            {
                rigid = go.AddComponent<Rigidbody>();
            }
            collider.isTrigger = true;
            rigid.useGravity = false;
            return collider;
        }

        public static bool IsThreat(BaseEntity entity)
        {
            return entity is Threat;
        }

        public static bool IsCoactive(BaseEntity entity)
        {
            return entity is Coactive;
        }

        public static bool IsReward(BaseEntity entity)
        {
            return entity is Reward;
        }
    }
}

