using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
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
    public LinkedList<HitAck> Confirmed = new LinkedList<HitAck>();
    public Thread recv;
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
            DontDestroyOnLoad(gameObject);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(new IPAddress(new byte[] { 0,0,0,0}), 28960));//listen on any address on this port

            recieving = true;
            recv = new Thread(()=> { Recieve(socket); });
            recv.Start();
            
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
                    if (Physics.Raycast(H.CameraLocation, H.normal)&&Players[H.playernum].damageTaken<100)
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
                while (beforeShot.Value.timeCreated>H.timeCreated&&beforeShot.Next.Next!=null)
                {
                    beforeShot = beforeShot.Next;

                }
                if (Players[H.hits[i]].PacketHistory.Count > 2)
                {
                    // same graph except this time we do not need to predict https://gyazo.com/f242ed66b95169f5cf85347ccee5f671
                    Player player = Players[H.hits[i]];
                    Vector3 n_nocross = (((MovementPacket)beforeShot.Value).position - ((MovementPacket)beforeShot.Next.Value).position);
                    Vector3 n = Vector3.Cross(n_nocross, Vector3.up);
                    Vector3 I = (((MovementPacket)beforeShot.Value).position - ((MovementPacket)beforeShot.Next.Next.Value).position);
                    Vector3 j = (((MovementPacket)player.PacketHistory.First.Next.Value).position - ((MovementPacket)player.PacketHistory.First.Next.Next.Value).position);
                    Vector3 PredictionPoint = ((MovementPacket)beforeShot.Previous.Value).position - ((MovementPacket)beforeShot.Value).position;
                    float avgSpeed = ((n_nocross.magnitude + j.magnitude) / (float)(((MovementPacket)beforeShot.Value).timeCreated - ((MovementPacket)beforeShot.Next.Next.Value).timeCreated).TotalSeconds);
                    Vector3 playerposition = ((MovementPacket)beforeShot.Value).position + (PredictionPoint * Mathf.Clamp(((float)(((H.timeCreated - beforeShot.Value.timeCreated).TotalSeconds)/ (beforeShot.Previous.Value.timeCreated - beforeShot.Value.timeCreated).TotalSeconds)), 0f, 1f));
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
    static void Recieve(Socket Forwarded) //constantly listen
    {
        byte[] b = new byte[1024];
        EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        Forwarded.ReceiveFrom(b,ref sender);
        PacketHandler.instance.Offload(b,(IPEndPoint)sender);
        if (recieving)//continue the thread if we aer still recieving
            Recieve(Forwarded);
       
    }
    private void FixedUpdate()
    {
        //every frame we start by clearing the buffer of all of its packets
        LinkedListNode<byte[]> buff = instance.Queue.Last;
        LinkedListNode<IPEndPoint> ep = instance.EndPoints.Last;
        for (int i = 0; i < instance.Queue.Count; i++)
        {
            try
            {
                BinaryFormatter b = new BinaryFormatter();
                MemoryStream m = new MemoryStream(buff.Value);
                var pack_ = b.Deserialize(m);
                
                
                if (pack_ is Packet) 
                {
                    PacketHandler.instance.ServerHandlePacket((Packet)pack_, ep);
                }
                //integrate the buffer and endpoint down the queue
                buff = buff.Previous;
                ep = ep.Previous;
                instance.Queue.RemoveLast();
                instance.EndPoints.RemoveLast();
            }
            catch (Exception e)
            {

            }
            
        }
        for (int x = 0; x < Players.Count; x++)
        {//make a fraghment for each player
            ServerFragment player = new ServerFragment();
            player.playernum = x;
            player.damageTaken = Players[x].damageTaken;
            LinkedListNode<Packet> lastMVPK = Players[x].PacketHistory.First;
            player.delay = (int)Players[x].Delay;
            player.timeCreated = lastMVPK.Value.timeCreated;
            player.position = ((MovementPacket)lastMVPK.Value).position;
            player.Rotation = ((MovementPacket)lastMVPK.Value).lookrotation;
            for (int y = 0; y < Players.Count; y++)
            {//send to each player
                socket.SendTo(player.toBytes(), Players[y].EndPoint);
            }
        }
       
        
        LinkedListNode<HitAck> current = Resolved.Last;
        for (int i = 0; i < Resolved.Count; i++)
        {
            instance.socket.SendTo(current.Value.toBytes(), current.Value.FromUser);
            LinkedListNode<HitAck> placeBefore = Confirmed.First;
            for (int j = 0; j < Confirmed.Count; j++)
            {
                if (current.Value.timeCreated >= placeBefore.Value.timeCreated && current.Value != placeBefore.Value)//happened after placebefore (most recent at front)
                {
                    Confirmed.AddBefore(placeBefore, current.Value);
                    break;
                }
                else if (current.Value == placeBefore.Value) 
                {//we have a copy
                    break;
                }
                else if (placeBefore.Next == null)
                {
                    Confirmed.AddAfter(placeBefore, current.Value);
                    break;
                }
                else 
                {//iterate here
                    placeBefore = placeBefore.Next;
                } 
            }
            
            current = current.Previous;
        }
        
    }
    private void Start()
    {
        
    }
    
}
