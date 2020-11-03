using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using UnityEditorInternal;
using UnityEngine;
//use push front for player packets to order for positional netcode
public class Server : MonoBehaviour
{
    static bool recieving = false;
    public Socket socket;
    public GameObject EnemyPrefab;//set in scene
    public List<Player> Players = new List<Player>();
    public LinkedList<IPEndPoint> EndPoints = new LinkedList<IPEndPoint>();
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
            Recieve();    
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
    public void ServerHandlePacket(Packet p, LinkedListNode<IPEndPoint> ep)
    {
        if (!Server.instance.Players[p.playernum].PacketHistory.Contains(p))
        {
            if (p is MovementPacket)//contains positional data
            {
                LinkedListNode<Packet> current;
                PacketHandler.placeInOrder(Server.instance.Players[p.playernum].PacketHistory, p, out current);//put it in the queue where we read from to determine position
                Server.instance.Players[p.playernum].EndPoint = ep.Value;//update the reference to the endpoint         
            }
            else if (p is HitAck)
            {
                if (Server.instance.Resolved.Contains((HitAck)p))//stop sending a resolved hit to the user once we get it back
                    Server.instance.Resolved.Remove((HitAck)p);
            }
            else if (p is HitPacket)
            {
                HitPacket P = (HitPacket)p;
                bool[] hitsP = Server.instance.TestHit(P);//test hit
                bool hit = false;
                for (int i = 0; i < hitsP.Length; i++)
                {// resolve hit here
                    if (hitsP[i])
                    {
                        hit = true;
                        Server.instance.Players[P.hits[i]].damageTaken += 20;//make this a delegate later?
                    }
                }
                HitAck hitConfirmation = new HitAck(P, hit);//create a hit confirmation and put it on the stack
                Server.instance.Resolved.AddFirst(hitConfirmation);
            }
            else if (p is ConnectionPacket)
            {//connect our gamer
                bool connected = false;
                ConnectionPacket connPacket = (ConnectionPacket)p;
                for (int y = 0; y < Server.instance.Players.Count; y++)
                {
                    if (Server.instance.Players[y].EndPoint == (ep.Value))
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
                connPacket.usernames = new string[Server.instance.Players.Count];//update the user list on the outgoing packet
                for (int i = 0; i < connPacket.usernames.Length; i++)
                {
                    connPacket.usernames[i] = Server.instance.Players[i].userName;
                }
                Server.instance.socket.SendTo((connPacket).toBytes(), ep.Value);//send back connection packet
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
                    Vector3 playerposition = ((MovementPacket)player.PacketHistory.First.Value).position + (PredictionPoint.normalized * avgSpeed * Mathf.Clamp(((float)(H.timeCreated - player.PacketHistory.First.Value.timeCreated).TotalSeconds), -1, .75f));
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
                    float avgSpeed = (((MovementPacket)beforeShot.Previous.Value).position - ((MovementPacket)beforeShot.Value).position).magnitude/ (float)(((MovementPacket)beforeShot.Previous.Value).timeCreated- ((MovementPacket)beforeShot.Value).timeCreated).TotalSeconds;
                    Vector3 playerposition = ((MovementPacket)beforeShot.Value).position + (nextPoint * Mathf.Clamp(((float)(((H.timeCreated - beforeShot.Value.timeCreated).TotalSeconds)/ (beforeShot.Previous.Value.timeCreated - beforeShot.Value.timeCreated).TotalSeconds)), 0f, .75f));
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
    void Recieve() //constantly listen
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
       
        socket.ReceiveTimeout = (1000);
        socket.Blocking = false;
        Task.Run(() =>
        {
            socket.Bind(new IPEndPoint(new IPAddress(new byte[] { 0, 0, 0, 0 }), 7777));//listen on any address on this port
            byte[] b = new byte[1024];
            EndPoint wanderingGamer = new IPEndPoint(IPAddress.Any, 0);
            while (recieving)
            {
                if (socket.Available > 0)
                {
                    socket.ReceiveFrom(b, ref wanderingGamer);
                    PacketHandler.instance.OffloadServer(b, (IPEndPoint)wanderingGamer);
                }
            }
        });

    }
    private void FixedUpdate()
    {
        //every frame we start by clearing the buffer of all of its packets
        LinkedListNode<byte[]> buff = instance.Queue.Last;
        LinkedListNode<IPEndPoint> ep = instance.EndPoints.Last;
        int count = instance.Queue.Count;//prevent modifying changing elements
        for (int i = 0; i < count; i++)
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
                instance.EndPoints.RemoveLast();
        }
        Player[] p = Players.ToArray();
        LinkedListNode<HitAck> current = Resolved.Last;
        count = Resolved.Count;
        Task.Run(() =>
        {
            for (int x = 0; x < p.Length; x++)
            {//make a fraghment for each player
                ServerFragment player = new ServerFragment();
                player.playernum = (short)x;
                player.damageTaken = (short)p[x].damageTaken;
                LinkedListNode<Packet> lastMVPK = p[x].PacketHistory.First;
                player.delay = (short)p[x].Delay;
                player.timeCreated = lastMVPK.Value.timeCreated;
                player.position = ((MovementPacket)lastMVPK.Value).position;
                player.Rotation = ((MovementPacket)lastMVPK.Value).lookrotation;
                for (int y = 0; y < Players.Count; y++)
                {//send to each player
                    socket.SendTo(player.toBytes(), Players[y].EndPoint);
                }
            }
            for (int i = 0; i < count; i++)
            {
                instance.socket.SendTo(current.Value.toBytes(), Players[current.Value.playernum].EndPoint);//send resolved packet until we get acknowledgement


                current = current.Previous;
            }
        });
        
    }
    private void Start()
    {
        
    }
    
}
