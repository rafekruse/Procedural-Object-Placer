using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InitializeColliders))]
public class CheckCollisions : MonoBehaviour {

    public bool ignoreEnvironmentCollisions = false;
    public bool isColliding = false;
    public int order;
    public bool invalid;

    private void OnTriggerStay(Collider other)
    {        
        isColliding = true;
        
        if ((ignoreEnvironmentCollisions && other.tag == "Ignorable") || other.transform == this.transform.parent)
        {
            isColliding = false;
        }
        if(other.transform.parent)
        {
            CheckCollisions otherCollisionScript = other.transform.parent.GetComponent<CheckCollisions>();
            if (otherCollisionScript && otherCollisionScript.order > order && !invalid)
            {
                isColliding = false;
            }
            else
            {
                invalid = true;
            }
        }

    }
    public void SetNewLocation(Vector3 position, Quaternion normalizedRotation)
    {
        transform.SetPositionAndRotation(position, normalizedRotation);
    }
}
