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
    public void clientHandlePacket(Packet p)
    {
        //if its a connection packet log the interaction
        if (p is ConnectionPacket) 
        {
            ConnectionPacket P = (ConnectionPacket)p;
            
            for (int i = 0; i < client.instance.Players.Count; i++)
            {
                client.instance.Players[i].userName = P.usernames[i];
            }
        }
        //if its a hit acknowledgement we send it back after removing the correct node from unconfirmed
        if (p is HitAck) 
        {
            HitAck P = (HitAck)p;
            LinkedListNode<HitPacket> unconfirmed = client.instance.unConfirmed.First;
            int j = client.instance.unConfirmed.Count;
            for (int i = 0; i < j; i++)
            {
                if (unconfirmed.Value.id == P.id)
                {

                    unconfirmed = unconfirmed.Next;
                    client.instance.unConfirmed.Remove(unconfirmed.Previous);
                    //hitmarker happens here
                    break;
                }
                else {
                    unconfirmed = unconfirmed.Next; }
            }
            client.instance.socket.SendTo(P.toBytes(), client.server);

        }
    }
    public void clientHandlePacket(ServerFragment s) 
    {
        while (s.playernum >= client.instance.Players.Count) //their playernum is higher than our max players
        {
            makeNewPlayerClient(s);
        }
        MovementPacket m = new MovementPacket(s.position,s.Rotation,s.playernum);//movement packet of the enemy
        m.timeCreated = s.timeCreated;//make sure they are identical
        LinkedListNode<Packet> node;
        placeInOrder(client.instance.Players[s.playernum].PacketHistory,m, out node);
        client.instance.Players[s.playernum].playernum = s.playernum;
        //client.instance.Players[s.playernum].Delay = s.delay; not implemented yet
        Enemy enemy = client.instance.Players[s.playernum].Dummy.GetComponent<Enemy>();//adjust the Enemy component
        enemy.username = client.instance.Players[s.playernum].userName;
        enemy.health = 100 - s.damageTaken; 
        
        
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
   
    public static void placeInOrder(LinkedList<Packet> l, Packet p, out LinkedListNode<Packet> current) 
    {
        LinkedListNode<Packet> node = l.First;
        while (p.timeCreated <= node.Value.timeCreated)//is our current node in the packet history younger than the packet
        {
            node = node.Next;//move further into the past
        }
        l.AddBefore(node, p);//add our packet right before the first packet that occured futher in the past
        current = node.Previous;
    }
    public void ServerHandlePacket(Packet p,LinkedListNode<IPEndPoint> ep) 
    {
        
        if (!Server.instance.Players[p.playernum].PacketHistory.Contains(p)&&p is MovementPacket)//contains positional data
        {
            LinkedListNode<Packet> current;
            placeInOrder(Server.instance.Players[p.playernum].PacketHistory, p, out current);
            Server.instance.Players[p.playernum].EndPoint = ep.Value;//update the reference to the endpoint         
        }
        if (p is HitPacket)
        {//add our hit to unresolved hits
            HitPacket P = (HitPacket)p;
            
            LinkedListNode<HitAck> curHitack = Server.instance.Unresolved.First;
            while (p.timeCreated <= curHitack.Value.timeCreated) { curHitack = curHitack.Next; }
            bool contained = (p.id == curHitack.Value.id);
            
            bool[] hitsP = Server.instance.TestHit(P);

            bool hit = false;
            for (int i = 0; i < hitsP.Length; i++)
            {
                if (hitsP[i])
                {
                    hit = true;
                    Server.instance.Players[P.hits[i]].damageTaken += 20;
                }
            }
            HitAck hitConfirmation = new HitAck(P,hit);//create a hit confirmation and put it on the stack
            
            Server.instance.Resolved.AddFirst(hitConfirmation);

        }
        if (p is ConnectionPacket)
        {//connect our gamer
        bool connected = false;
        ConnectionPacket P = (ConnectionPacket)p;
        Player Gamer = new Player();
        for (int y = 0; y < Server.instance.Players.Count; y++)
        {
            if (Server.instance.Players[y].EndPoint == (ep.Value))
            {
                Server.instance.Players[y].Delay = 2f * (float)(DateTime.Now - p.timeCreated).TotalMilliseconds;//set delay
                P.playernum = (short)y;
                
                connected = true;//gamer is already here update delay
            }
        }
        if (!connected)
        {
            Gamer.Delay = 2f * (float)(DateTime.Now - p.timeCreated).TotalMilliseconds;
            Gamer.damageTaken = 0;
            Gamer.EndPoint = ep.Value;
            Gamer.Dummy = Server.instance.EnemyPrefab;//initialize new player
            Gamer.PacketHistory = new LinkedList<Packet>();
            Gamer.userName = P.username;
            Gamer.playernum = (short)Server.instance.Players.Count;
            P.playernum = (short)Server.instance.Players.Count;
            Server.instance.Players.Add(Gamer);
            
        }
        P.usernames = new string[Server.instance.Players.Count];
        for (int i = 0; i < P.usernames.Length; i++)
        {
            P.usernames[i] = Server.instance.Players[i].userName; 
        }
        Server.instance.socket.SendTo((P).toBytes(), ep.Value);//send back connection packet
        }
        if (p is HitAck) 
        {
            if(Server.instance.Resolved.Contains((HitAck)p))
            Server.instance.Resolved.Remove((HitAck)p);            
        }

    }

}
    
    // Update is called once per frame
   

