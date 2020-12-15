using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEngine;
//use push front for player packets to order for positional netcode
public class Server : MonoBehaviour
{
    static bool recieving = false;
    public Socket socket;
    public GameObject EnemyPrefab;//set in scene
    public List<Player> Players = new List<Player>();
    public LinkedList<CSteamID> SteamIDs = new LinkedList<CSteamID>();
    public LinkedList<byte[]> Queue = new LinkedList<byte[]>();
    public LinkedList<HitAck> Resolved = new LinkedList<HitAck>();
    public LinkedList<HitAck> Unresolved = new LinkedList<HitAck>();
    public static Server instance;
    // Start is called before the first frame update
    void Awake()//set the server singleton
    {
        if (instance != null && instance != this)
        {
            gameObject.SetActive(false);
        }
        else
        {
            instance = this;
            recieving = true;   
        }
        
    }
    private void OnDestroy()
    {
        recieving = false;
    }
    private void OnApplicationQuit()
    {
        recieving = false;
    }
    public virtual void resolveConfirmPacket(ConfirmPacket c) 
    {
        if (c is HitPacket)
        {
            HitPacket P = (HitPacket)c;
            HitAck hitConfirmation;
            HitResolution(P, out hitConfirmation);
            Server.instance.Resolved.AddFirst(hitConfirmation);
        }
    }
    public virtual void HitResolution(HitPacket H,out HitAck h) 
    {
        bool[] hitsP = Server.instance.TestHit(H);//test hit
        bool hit = false;
        for (int i = 0; i < hitsP.Length; i++)
        {// resolve hit here
            if (hitsP[i])
            {
                hit = true;
                Server.instance.Players[H.hits[i]].damageTaken += 20;//make this a delegate later?
            }
        }
        h = new HitAck(H, hit);
    }
    public void ServerHandlePacket(Packet p, LinkedListNode<CSteamID> ep)
    {
        if (p is ConnectionPacket)
        {//connect our gamer
            
            bool connected = false;
            ConnectionPacket connPacket = (ConnectionPacket)p;
            for (int y = 0; y < Server.instance.Players.Count; y++)
            {
                if (Server.instance.Players[y].SteamID == (ep.Value))
                {
                    Server.instance.Players[y].Delay = 2f * (float)(DateTime.Now - p.timeCreated).TotalMilliseconds;//set delay
                    connPacket.playernum = (short)y;
                    connected = true;//gamer is already here update delay
                }
            }
            if (!connected)//if the player is not in our list of players make a new one
            {
                PacketHandler.makeNewPlayerServer(connPacket, ep);
            }
            connPacket.usernames = new string[12];//update the user list on the outgoing packet
            for (int i = 0; i < Server.instance.Players.Count; i++)
            {
                connPacket.usernames[i] = Server.instance.Players[i].userName;
            }
            SteamNetworking.SendP2PPacket(ep.Value, PacketHandler.toClient(connPacket.toBytes()), (uint)PacketHandler.toClient(connPacket.toBytes()).Length, EP2PSend.k_EP2PSendUnreliableNoDelay);
            
        }
        else if (p.playernum != 1000)
        {
            if (!Server.instance.Players[p.playernum].PacketHistory.Contains(p))
            {
                if (p is MovementPacket)//contains positional data
                {
                    LinkedListNode<Packet> current;
                    PacketHandler.placeInOrder(Server.instance.Players[p.playernum].PacketHistory, p, out current);//put it in the queue where we read from to determine position
                    Server.instance.Players[p.playernum].SteamID = ep.Value;//update the reference to the endpoint

                }
                else if (p is HitAck)
                {
                    if (Server.instance.Resolved.Contains((HitAck)p))//stop sending a resolved hit to the user once we get it back
                        Server.instance.Resolved.Remove((HitAck)p);
                }
                else if (p is ConfirmPacket)
                {
                    //resolveConfirmPacket()
                    
                     
                    
                }


            }
        }

    }
    public bool[] TestHit(HitPacket H)//returns an array of which enemies we hit with a given hitpacket
    {
        bool[] Confirms = new bool[H.hits.Length];
        List<GameObject> Enemies = new List<GameObject>();
        for (int i = 0; i < H.hits.Length; i++)
        {

            if (Players[H.hits[i]].PacketHistory.First.Value.timeCreated <= H.timeCreated)//if the most recent node is before our shot we predict and extrapolate
            {
                if (Players[H.hits[i]].PacketHistory.Count > 2)
                {
                    // this graph explains this block https://gyazo.com/f242ed66b95169f5cf85347ccee5f671
                    Player player = Players[H.hits[i]];
                    Vector3 n_nocross = (((MovementPacket)player.PacketHistory.First.Value).position - ((MovementPacket)player.PacketHistory.First.Next.Value).position);
                    Vector3 n = Vector3.Cross(n_nocross, Vector3.up);
                    Vector3 I = (((MovementPacket)player.PacketHistory.First.Value).position - ((MovementPacket)player.PacketHistory.First.Next.Next.Value).position);
                    Vector3 j = (((MovementPacket)player.PacketHistory.First.Next.Value).position - ((MovementPacket)player.PacketHistory.First.Next.Next.Value).position);
                    Vector3 PredictionPoint = (I - 2 * n * Vector3.Dot(I, n));
                    float avgSpeed = (n_nocross.magnitude + j.magnitude) / (float)(((MovementPacket)player.PacketHistory.First.Value).timeCreated - ((MovementPacket)player.PacketHistory.First.Next.Next.Value).timeCreated).TotalSeconds;
                    Vector3 playerposition = ((MovementPacket)player.PacketHistory.First.Value).position + (PredictionPoint * Mathf.Clamp(((float)(H.timeCreated - player.PacketHistory.First.Value.timeCreated).TotalSeconds), -1, 10f));
                    GameObject temp = Instantiate(EnemyPrefab);
                    temp.transform.position = playerposition;
                    temp.transform.rotation = ((MovementPacket)player.PacketHistory.First.Value).lookrotation * Quaternion.Euler((((MovementPacket)player.PacketHistory.First.Next.Value).lookrotation.eulerAngles - ((MovementPacket)player.PacketHistory.First.Value).lookrotation.eulerAngles));
                    if (Physics.Raycast(H.CameraLocation, H.normal)&&Players[H.playernum].damageTaken<100)//needs work
                    {
                        Confirms[i] = true;
                    }
                    else
                    {
                        Confirms[i] = false;
                    }
                    GameObject.Destroy(temp);
                }
                else
                {
                    Confirms[i] = true;
                }
            }
            else //otherwise we interpolate between the two points based on current time
            {
                LinkedListNode<Packet> beforeShot = Players[H.hits[i]].PacketHistory.First;
                while (beforeShot.Value.timeCreated >= H.timeCreated&&beforeShot.Next.Next!=null)
                {
                    beforeShot = beforeShot.Next;

                }
                if (Players[H.hits[i]].PacketHistory.Count > 2)
                {
                    // same graph except this time we do not need to predict https://gyazo.com/f242ed66b95169f5cf85347ccee5f671
                    Player player = Players[H.hits[i]];
                    Vector3 n_nocross = (((MovementPacket)beforeShot.Value).position - ((MovementPacket)beforeShot.Next.Value).position);
                    Vector3 j = (((MovementPacket)player.PacketHistory.First.Next.Value).position - ((MovementPacket)player.PacketHistory.First.Next.Next.Value).position);
                    Vector3 nextPoint = ((MovementPacket)beforeShot.Previous.Value).position - ((MovementPacket)beforeShot.Value).position;
                    //float avgSpeed = (((MovementPacket)beforeShot.Previous.Value).position - ((MovementPacket)beforeShot.Value).position).magnitude/ (float)(((MovementPacket)beforeShot.Previous.Value).timeCreated- ((MovementPacket)beforeShot.Value).timeCreated).TotalSeconds;
                    float timeCoeff = (float)((DateTime.Now) - player.PacketHistory.First.Value.timeCreated).TotalSeconds / (float)(player.PacketHistory.First.Value.timeCreated - player.PacketHistory.First.Next.Next.Value.timeCreated).TotalSeconds;
                    Vector3 playerposition = ((MovementPacket)beforeShot.Value).position + (nextPoint * Mathf.Clamp(timeCoeff, 0f, 20f));
                    GameObject temp = Instantiate(player.Dummy);
                    temp.transform.position = playerposition;
                    temp.transform.rotation = ((MovementPacket)player.PacketHistory.First.Value).lookrotation * Quaternion.Euler((((MovementPacket)player.PacketHistory.First.Next.Value).lookrotation.eulerAngles - ((MovementPacket)player.PacketHistory.First.Value).lookrotation.eulerAngles));
                    if (Physics.Raycast(H.CameraLocation, H.normal))
                    {
                        Confirms[i] = true;
                    }
                    else
                    {
                        Confirms[i] = false;
                    }
                    GameObject.Destroy(temp);
                }
                else
                {
                    Confirms[i] = true;
                }
            }
        }
        return Confirms;
    }
    
    public void FixedUpdate()
    {
        
        //every frame we start by clearing the buffer of all of its packets
        LinkedListNode<byte[]> buff = instance.Queue.Last;
        LinkedListNode<CSteamID> ep = instance.SteamIDs.Last;
        int count = 0;
        if(buff != null) 
        { 
        count = instance.Queue.Count;//prevent modifying changing elements
        }
        for (int i = 0; i < count-1; i++)
        {
           
            try
            {
                BinaryFormatter b = new BinaryFormatter();
                MemoryStream m = new MemoryStream(buff.Value);
                var pack_ = b.Deserialize(m);
                if (pack_ is Packet)
                {
                    ServerHandlePacket((Packet)pack_, ep);
                }
                //integrate the buffer and endpoint down the queue
                buff = buff.Previous;
                ep = ep.Previous;
                instance.Queue.RemoveLast();
                instance.SteamIDs.RemoveLast();
            }
            catch (Exception)
            {

            }
               
        }
        Player[] p = Players.ToArray();
        LinkedListNode<HitAck> current = Resolved.Last;
        count = Resolved.Count;

        
            for (int x = 0; x < p.Length; x++)
            {//make a fraghment for each player
                ServerFragment player = new ServerFragment();
                player.playernum = (short)x;
                player.damageTaken = (short)p[x].damageTaken;
                LinkedListNode<Packet> lastMVPK = p[x].PacketHistory.First;
                if (lastMVPK != null)
                {
                    player.delay = (short)p[x].Delay;
                    player.timeCreated = lastMVPK.Value.timeCreated;
                    player.position = ((MovementPacket)lastMVPK.Value).position;
                    player.Rotation = ((MovementPacket)lastMVPK.Value).lookrotation;
                    player.timeCreated = lastMVPK.Value.timeCreated;
                    for (int y = 0; y < Players.Count; y++)
                    {//send to each player
                        if (y == player.playernum) 
                        {
                            player.position = new Vector3(player.position.x * -1,player.position.y,player.position.z * -1);
                            player.playernum = (short)(player.playernum + 1);
                            
                        }
                    //print("sending player["+player.playernum+"]'s position: "+player.position+" to "+Players[y].EndPoint.Address+" "+Players[y].EndPoint.Port);

                    SteamNetworking.SendP2PPacket(Players[y].SteamID, PacketHandler.toClient(player.toBytes()), (uint)PacketHandler.toClient(player.toBytes()).Length, EP2PSend.k_EP2PSendUnreliableNoDelay);
                }
                }


                for (int i = 0; i < count; i++)
                {
                SteamNetworking.SendP2PPacket(Players[current.Value.playernum].SteamID, PacketHandler.toClient(current.Value.toBytes()), (uint)PacketHandler.toClient(current.Value.toBytes()).Length, EP2PSend.k_EP2PSendUnreliableNoDelay);
                

                current = current.Previous;
                }
            }
        
        
    }
    private void Start()
    {
        
    }
    
}