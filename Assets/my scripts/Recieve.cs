using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recieve : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    void RecieveCall() //constantly listen
    {
        byte[] b = new byte[1024];
        CSteamID wanderingGamer;
        uint msgsize = 0;

        



            while (SteamNetworking.ReadP2PPacket(b, (uint)b.Length, out msgsize, out wanderingGamer))
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
    // Update is called once per frame
    void FixedUpdate()
    {
        RecieveCall();
    }
}
