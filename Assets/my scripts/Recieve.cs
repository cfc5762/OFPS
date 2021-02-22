using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recieve : MonoBehaviour
{
    public static void OnP2PConnectionFailed(SteamId id, P2PSessionError error)
        {
            Debug.Log(id + " " + error.ToString());
        }
    static void OnP2PSessionRequest(SteamId id) 
    {
        SteamNetworking.AcceptP2PSessionWithUser(id);
    }

    void Start()
    {
        // setup the callback method
        SteamNetworking.OnP2PSessionRequest += OnP2PSessionRequest;
        SteamNetworking.OnP2PConnectionFailed += OnP2PConnectionFailed;
    }
    
    void RecieveCall() //constantly listen
    {

        uint msgsize = 0;
        while (SteamNetworking.IsP2PPacketAvailable())
        {
            var b = new byte[512];
            SteamId wanderingGamer = 0;
            if (PacketHandler.instance != null) 
            { 
            if (SteamNetworking.ReadP2PPacket(b, ref msgsize, ref wanderingGamer, 0))
            {
                

                if (b != null)
                {
                    if (b[0] == 200)
                    {

                        PacketHandler.instance.OffloadClient(PacketHandler.fromClient(b), wanderingGamer);
                    }
                    else if (b[0] == 100)
                    {
                            
                        PacketHandler.instance.OffloadServer(PacketHandler.fromServer(b), wanderingGamer);
                    }
                }
            }
        }
            else
            {
                Debug.Log("Waiting on packethandler");
            }
        }
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        RecieveCall();
    }
}
