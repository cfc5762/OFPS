using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestHost : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        client.username = "silktail";
        client.server = new Steamworks.CSteamID();//temp initialization
        
        SceneManager.LoadScene(0);//loadclienttest
       
        //3 should be a scene with this in it
    }                  
    // Update is called once per frame
    void Update()
    {
        
    }
}
