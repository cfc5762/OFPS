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
        client.server = new IPEndPoint(new IPAddress(new byte[] { 127,0,0,1}), 28960);//temp initialization
        SceneManager.LoadScene(0);//loadclienttest
        SceneManager.LoadSceneAsync(1);//loadservertest
        //SceneManager.LoadSceneAsync(2);//loaddummyclient
        //3 should be a scene with this in it
    }                  
    // Update is called once per frame
    void Update()
    {
        
    }
}
