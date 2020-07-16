using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCollision : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    public void PushBody(Rigidbody r)
    {
       
        r.transform.position += new Vector3((r.position - transform.position).x, 0, (r.position - transform.position).z).normalized*.2f;

    }
}
