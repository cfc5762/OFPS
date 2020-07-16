using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam : MonoBehaviour
{
    // Start is called before the first frame update

    private void Awake()
    {
        client.cam = transform;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
