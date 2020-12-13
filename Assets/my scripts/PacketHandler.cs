using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;
using Steamworks;

public class PacketHandler : MonoBehaviour
{
    public static byte[] fromClient(byte[] b)
    {
        List<byte> b2 = b.ToList<byte>();
        b2.RemoveAt(0);
        return b2.ToArray();
    }
    public static byte[] fromServer(byte[] b)
    {
        List<byte> b2 = b.ToList<byte>();
        b2.RemoveAt(0);
        return b2.ToArray();
    }
    public static byte[] toClient(byte[] b)
    {
        List<byte> b2 = b.ToList<byte>();
        b2.Insert(0, 200);
        return b2.ToArray();
    }
    public static byte[] toServer(byte[] b)
    {
        List<byte> b2 = b.ToList<byte>();
        b2.Insert(0, 100);
        return b2.ToArray();
    }
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
    public void OffloadClient(byte[] P, CSteamID e)
    {
        if (P != null && e != null)
        {
            if (e == client.server)
            {//if we got it from the server offload 
                client.instance.Queue.AddFirst(P);
            }
            
        }
    }
    public void OffloadServer(byte[] P, CSteamID e)
    {
        Server.instance.SteamIDs.AddFirst(e);
        Server.instance.Queue.AddFirst(P);
    }
    public static void makeNewPlayerClient(ConnectionPacket P)
    {
        Debug.Log("making "+P.username+" for the client");
        Player player = new Player();
        if (client.myPlayerNum != P.playernum)
        {
            player.Dummy = Instantiate(Server.instance.EnemyPrefab);
            player.SteamID = client.server;
            player.playernum = P.playernum;
        }
        client.instance.Players.Add(player);
    }
    public static void makeNewPlayerClient(ServerFragment s) 
    {
        Debug.Log("making new player for the client");
        Player player = new Player();
        player.SteamID = client.server;
        player.playernum = client.instance.Players.Count;
        player.Dummy = Instantiate(client.instance.EnemyPrefab);
        player.Dummy.transform.position = new Vector3(0, 1000, 0);
        player.playernum = s.playernum;
        client.instance.Players.Add(player);
    }
    public static void makeNewPlayerServer(ConnectionPacket P , LinkedListNode<CSteamID> ep) 
    {
        Player Gamer = new Player();
        Gamer.Delay = 2f * (float)(DateTime.Now - P.timeCreated).TotalMilliseconds;
        Gamer.damageTaken = 0;
        Gamer.SteamID = ep.Value;
        Gamer.Dummy = Server.instance.EnemyPrefab;//initialize new player
        Gamer.PacketHistory = new LinkedList<Packet>();
        Gamer.userName = P.username;
        Gamer.playernum = (short)Server.instance.Players.Count;
        P.playernum = (short)Server.instance.Players.Count;
        Server.instance.Players.Add(Gamer);
    }
    public static void placeInOrderUnique(LinkedList<Packet> l, Packet p, out LinkedListNode<Packet> current)
    {
        bool addBefore = true;
        current = null;
        LinkedListNode<Packet> node = l.First;
        if (node != null)
        {
            while (p.timeCreated <= node.Value.timeCreated)//is our current node in the packet history younger than the packet
            {
                if (node.Next != null)
                    node = node.Next;//move further into the past
                else
                {
                    addBefore = false;
                    break;
                }
            }
            if (addBefore)
            {
                if (node.Previous == null)
                {

                    l.AddBefore(node, p);//add our packet right before the first packet that occured futher in the past
                    current = node.Previous;
                }
                else if (node.Previous.Value.timeCreated.Ticks != p.timeCreated.Ticks)
                {
                    l.AddBefore(node, p);//add our packet right before the first packet that occured futher in the past
                    current = node.Previous;
                }
                else 
                {
                //print("trash")
                }
            }
            else
            {
                current = node;
                l.AddAfter(node, p);
            }
        }
        else
        {
            l.AddFirst(p);
        }


    }

    public static void placeInOrder(LinkedList<Packet> l, Packet p, out LinkedListNode<Packet> current) 
    {
        bool addBefore = true;
        current = null;
        LinkedListNode<Packet> node = l.First;
        if (node != null)
        {
            while (p.timeCreated <= node.Value.timeCreated)//is our current node in the packet history younger than the packet
            {
                if (node.Next != null)
                    node = node.Next;//move further into the past
                else 
                {
                    addBefore = false;
                    break;
                }
            }
            if (addBefore)
            {
                l.AddBefore(node, p);//add our packet right before the first packet that occured futher in the past
                current = node.Previous;
            }
            else 
            {
                l.AddAfter(node, p);
            }
        }
        else 
        {
            l.AddFirst(p);
        }
        
        
    }
   

}
    
    // Update is called once per frame
   



