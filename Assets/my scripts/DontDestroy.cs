using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    public static List<GameObject> Destroy;
    public static void Des() 
    {
        foreach (var item in Destroy)
        {
            GameObject.Destroy(item);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        if (Destroy == null) 
        {
            Destroy = new List<GameObject>();
        }
        Destroy.Add(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
