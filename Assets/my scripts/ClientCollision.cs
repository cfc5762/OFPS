using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientCollision : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<EnemyCollision>()) 
        {
            other.gameObject.GetComponent<EnemyCollision>().PushBody(GetComponent<Rigidbody>());
        }
    }
}
