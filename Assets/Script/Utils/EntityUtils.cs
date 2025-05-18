using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityUtils 
{
    public static Collider CreateCollider(GameObject go)
    {
        Collider collider = go.GetComponent<Collider>();
        Rigidbody rigid = go.GetComponent<Rigidbody>(); ;
        if (collider == null)
        {
            collider = go.AddComponent<BoxCollider>();
            rigid = go.AddComponent<Rigidbody>();
        }
        collider.isTrigger = true;
        rigid.useGravity = false;
        return collider;
    }
}
