using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityStandardAssets.SceneUtils;

public class PacketHandler : MonoBehaviour
{
    
    //make a list for ur packets
    public static PacketHandler instance;
    // Start is called before the first frame update
    void Awake()
    {
        if (instance != null && instance != this)
        {
            gameObject.SetActive(false);
        }
        else
        {
            instance = this;
        }
    }
    public void OffloadClient(byte[] P, IPEndPoint e) 
    {
        if (P != null && e != null)
        {
            if (e == client.server)
            {//if we got it from the server offload 
                client.instance.Queue.AddFirst(P);
            }
        }
    }
    public void OffloadServer(byte[] P,IPEndPoint e)
    {//offload with endpoints to the queue to be processed
        Server.instance.EndPoints.AddFirst((IPEndPoint)e);
        Server.instance.Queue.AddFirst(P);
    }
    public static void makeNewPlayerClient(ServerFragment s) 
    {
        print("making new player for the client");
        Player player = new Player();
        player.EndPoint = client.server;
        player.playernum = client.instance.Players.Count;
        player.Dummy = Instantiate(client.instance.EnemyPrefab);
        player.Dummy.transform.position = new Vector3(0, 1000, 0);
        player.playernum = s.playernum;
        client.instance.Players.Add(player);
    }
    public static void makeNewPlayerServer(ConnectionPacket P , LinkedListNode<IPEndPoint> ep) 
    {
        Player Gamer = new Player();
        Gamer.Delay = 2f * (float)(DateTime.Now - P.timeCreated).TotalMilliseconds;
        Gamer.damageTaken = 0;
        Gamer.EndPoint = ep.Value;
        Gamer.Dummy = Server.instance.EnemyPrefab;//initialize new player
        Gamer.PacketHistory = new LinkedList<Packet>();
        Gamer.userName = P.username;
        Gamer.playernum = (short)Server.instance.Players.Count;
        P.playernum = (short)Server.instance.Players.Count;
        Server.instance.Players.Add(Gamer);
    }
   
    public static void placeInOrder(LinkedList<Packet> l, Packet p, out LinkedListNode<Packet> current) 
    {
        LinkedListNode<Packet> node = l.First;
        while (p.timeCreated >= node.Value.timeCreated)//is our current node in the packet history younger than the packet
        {
            node = node.Next;//move further into the past
        }
        l.AddBefore(node, p);//add our packet right before the first packet that occured futher in the past
        current = node.Previous;
    }
   

}
    
    // Update is called once per frame
   

