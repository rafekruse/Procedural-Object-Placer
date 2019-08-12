using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitializeColliders : MonoBehaviour {

    private void Awake()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach(Collider col in colliders)
        {
            col.isTrigger = true;
        }

        Rigidbody rb;
        rb = GetComponent<Rigidbody>() == null ? gameObject.AddComponent<Rigidbody>() : GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }
}
