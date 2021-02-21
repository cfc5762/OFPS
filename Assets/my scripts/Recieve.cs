using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recieve : MonoBehaviour
{
    public static Callback<P2PSessionRequest_t> _p2PSessionRequestCallback;
    public static Callback<P2PSessionConnectFail_t> _p2PSessionRequestFail;

    void Start()
    {
        // setup the callback method
        _p2PSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
        _p2PSessionRequestFail = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionFail);
    }
    void OnP2PSessionFail(P2PSessionConnectFail_t request)
    {
        Debug.Log("got request fail - " + request.m_eP2PSessionError.ToString());
        CSteamID clientId = request.m_steamIDRemote;
        SteamNetworking.AcceptP2PSessionWithUser(clientId);
    }
    void OnP2PSessionRequest(P2PSessionRequest_t request)
    {
        Debug.Log("got request " + request.ToString());
        CSteamID clientId = request.m_steamIDRemote;
        SteamNetworking.AcceptP2PSessionWithUser(clientId);
    }
    void RecieveCall() //constantly listen
    {

        uint msgsize = 0;
        while (SteamNetworking.IsP2PPacketAvailable(out msgsize))
        {
            Debug.Log("got a msg");
            var b = new byte[msgsize];
            uint bytesRead;
            CSteamID wanderingGamer;
            if (SteamNetworking.ReadP2PPacket(b, msgsize, out bytesRead, out wanderingGamer))
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
    // Update is called once per frame
    void FixedUpdate()
    {
        RecieveCall();
    }
}
