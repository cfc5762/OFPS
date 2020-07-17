using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;

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
            //DontDestroyOnLoad(gameObject);
        }
    }
    public void OffloadClient(byte[] P, IPEndPoint e) 
    {
        if (e == client.server) 
        {//if we got it from the server offload 
            client.instance.Queue.AddFirst(P);
        }
    }
    public void Offload(byte[] P,IPEndPoint e)
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
            Player player = new Player();
            player.EndPoint = client.server;
            player.playernum = client.instance.Players.Count;
            player.Dummy = Instantiate(client.instance.EnemyPrefab);
            player.Dummy.transform.position = new Vector3(0, 1000, 0);
        }
        MovementPacket m = new MovementPacket(s.position,s.Rotation,s.playernum);//movement packet of the enemy
        m.timeCreated = s.timeCreated;//make sure they are identical
        client.instance.Players[s.playernum].PacketHistory.AddFirst(m);//add it to the players packethistory
        client.instance.Players[s.playernum].Delay = s.delay;
        Enemy enemy = client.instance.Players[s.playernum].Dummy.GetComponent<Enemy>();//adjust the Enemy component
        enemy.username = client.instance.Players[s.playernum].userName;
        enemy.health = 100 - s.damageTaken; 
        
        
    }
    public void ServerHandlePacket(Packet p,LinkedListNode<IPEndPoint> ep) 
    {
        p.FromUser = ep.Value;
        if (!Server.instance.Players[p.playernum].PacketHistory.Contains(p)&&p is MovementPacket)//contains positional data
        {
            LinkedListNode<Packet> current = Server.instance.Players[p.playernum].PacketHistory.First;
            while (p.timeCreated<=current.Value.timeCreated)
            {
                current = current.Next;
            }
            Server.instance.Players[p.playernum].PacketHistory.AddBefore(current, p);
        }
        
        if (p is MovementPacket) //this is exacly what it looks like
        {

            MovementPacket move = (MovementPacket)p;
            LinkedListNode<Packet> node = Server.instance.Players[move.playernum].PacketHistory.First;
            while (node.Value.timeCreated >= move.timeCreated && node.Next != null)
                {//keep scrolling till we find a node that happened before our movement
                    node = node.Next;
                }
            if (node.Value.timeCreated < move.timeCreated)
                {//insert our movement
                    Server.instance.Players[move.playernum].PacketHistory.AddAfter(node, move);
                }
        }
        if (p is HitPacket)
        {//add our hit to unresolved hits
            HitPacket P = (HitPacket)p;
            
            LinkedListNode<HitAck> curHitack = Server.instance.Confirmed.First;
            while (p.timeCreated < curHitack.Value.timeCreated) { curHitack = curHitack.Next; }
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
                P.playernum = y;
                connected = true;
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
            P.playernum = Server.instance.Players.Count;
            Server.instance.Players.Add(Gamer);
            Server.instance.Players[Server.instance.Players.Count - 1].playernum = Server.instance.Players.Count - 1;
            
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
   

