using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam : MonoBehaviour
{
    // Start is called before the first frame update

    private void Awake()
    {
       
    }
    // Update is called once per frame
    void Update()
    {
        if (!client.cam) 
        {
            client.cam = transform;
        }
    }
}
