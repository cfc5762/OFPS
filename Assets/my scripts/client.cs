using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class client : MonoBehaviour
{
    public static Thread sendThread;
    public static LinkedList<Tuple<Socket,byte[], EndPoint>> toSendQueue;
    public static int myPlayerNum;
    public LinkedList<HitPacket> unConfirmed;
    public LinkedList<byte[]> Queue = new LinkedList<byte[]>();
    public bool playing;
    public GameObject FpsController;
    public static Transform cam;
    public GameObject EnemyPrefab;
    public ConnectionPacket lastConnectionPacket;//all statics except for instance are to be set outside the script
    public static client instance;
    public IPEndPoint localEp;
    public static IPEndPoint server;//needs to be set outside of this script before scene load
    public static string username;//needs to be set outside of this script before scene load
    Thread recieveThread;
    public Socket socket;
    public List<Player> Players = new List<Player>();
    public static IPAddress GetLocalIPAddress()
    {
        if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            return new IPAddress(new byte[] { 127, 0, 0, 1 });
        }
        return new IPAddress(new byte[] { 127, 0, 0, 1 });
    }
    // Start is called before the first frame update
    void Awake()
    {

        client.username = "silktail";
        client.server = new IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), 28960);//temp initialization
        localEp = new IPEndPoint(new IPAddress(new byte[] { 127,0,0,1}), 0);//temp initialization
        if (instance != null && instance != this)
        {
            gameObject.SetActive(false);//ensure only one instance
        }
        else
        {   
            unConfirmed = new LinkedList<HitPacket>();
            socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram, ProtocolType.Udp);//declare it like this and only like this lest ye be cursed with ipv6
            toSendQueue = new LinkedList<Tuple<Socket, byte[], EndPoint>>();
            playing = true;
            instance = this;
            socket.Bind(localEp);
            StartCoroutine(ConnectionTick());
            StartCoroutine(MovementTick()); 
            recieveThread = new Thread(() => { Recieve(); });
            recieveThread.Start();
            sendThread = new Thread(() => { Send(); });
            sendThread.Start();



        }
    }
    void Send() 
    {
        while (playing)
        {
            int i = 0;
            while (toSendQueue.Last != null)
            {
                i++;
                if (i > 100)
                {
                    break;
                }
                toSendQueue.Last.Value.Item1.SendTo(toSendQueue.Last.Value.Item2, toSendQueue.Last.Value.Item3);
                try
                {
                    toSendQueue.RemoveLast();
                }
                catch (Exception ex)
                {

                    
                }
                

            }

            Thread.Sleep(17);//note dont move this shit or it will crash -love brain
        }
        
    }
    void SendTo(Socket sock, byte[] b, EndPoint ep) 
    {
        Thread d = new Thread(() => {
            sock.SendTo(b, ep); 
        });    
    }
    private void OnDestroy()
    {
        playing = false;
    }
    private void OnApplicationQuit()
    {
        playing = false;
    }
    public static LinkedListNode<Tuple<Socket, byte[], EndPoint>> getNode(Socket so, byte[] by, EndPoint ep) 
    {
        return new LinkedListNode<Tuple<Socket, byte[], EndPoint>>(new Tuple<Socket, byte[], EndPoint>(so, by, ep));
    }
    IEnumerator ConnectionTick() 
    {
        while (playing) 
        {
            if (lastConnectionPacket != null)
            {
                //SendTo(socket, (new ConnectionPacket(username, lastConnectionPacket.playernum)).toBytes(), server);
                
                toSendQueue.AddFirst(getNode(socket, (new ConnectionPacket(username, lastConnectionPacket.playernum)).toBytes(),server));
            }
            else 
            {
                EndPoint e = new IPEndPoint(GetLocalIPAddress(), 28960);
                //SendTo(socket, (new ConnectionPacket(username)).toBytes(), server);
               
                toSendQueue.AddFirst(getNode(socket, (new ConnectionPacket(username)).toBytes(), server));
            }
            yield return new WaitForSeconds(3);
        }    
    }
    IEnumerator MovementTick()
    {
        while (playing) 
        {
            if (lastConnectionPacket!=null) {
                MovementPacket movement = new MovementPacket(FpsController.transform, lastConnectionPacket.playernum);//construct a movement packet out of our player
                LinkedListNode<HitPacket> shot = unConfirmed.Last;
                for (int i = 0; i < unConfirmed.Count; i++)//send all unconfirmed shots
                {
                    toSendQueue.AddFirst(getNode(socket, shot.Value.toBytes(), server));
                   
                    if (shot.Previous == null)
                        break;
                    shot = shot.Previous;
                }
                toSendQueue.AddFirst(getNode(socket, movement.toBytes(), server));//send movement packet
                //socket.SendTo(movement.toBytes(), server);
            }
            yield return new WaitForSeconds(1/120f);
        }
    }
    void Recieve()
    {

        while (playing)
        {
            byte[] b = new byte[2048];
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint senderRemote = (EndPoint)sender;

            socket.ReceiveFrom(b, ref senderRemote);
            print("got");
            PacketHandler.instance.OffloadClient(b, (IPEndPoint)senderRemote);
        }
        
    }
    private void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(0)&&client.cam != null) 
        {
            RaycastHit hit;
            if (Physics.Raycast(cam.position, cam.forward, out hit))
            { 
                if (hit.distance <= 50) 
                {
                    //splatter effect
                    if (hit.transform.gameObject.GetComponent<Enemy>()!=null) 
                    {
                        Enemy Shot = hit.transform.gameObject.GetComponent<Enemy>();
                        HitPacket h = new HitPacket(cam, instance.gameObject.transform, new int[] { Shot.playernum });
                        unConfirmed.AddFirst(h);
                    }
                }
            }

        }
    }
    // Update is called once per frame
    void Update()
    {
        foreach (Player player in Players.ToArray())
        {
            if (player.playernum != lastConnectionPacket.playernum)
            {
                if (player.PacketHistory.Count > 2)
                {

                    Vector3 n_nocross = (((MovementPacket)player.PacketHistory.First.Value).position - ((MovementPacket)player.PacketHistory.First.Next.Value).position);
                    Vector3 n = Vector3.Cross(n_nocross, Vector3.up);
                    Vector3 i = (((MovementPacket)player.PacketHistory.First.Value).position - ((MovementPacket)player.PacketHistory.First.Next.Next.Value).position);
                    Vector3 j = (((MovementPacket)player.PacketHistory.First.Next.Value).position - ((MovementPacket)player.PacketHistory.First.Next.Next.Value).position);
                    Vector3 PredictionPoint = (i - 2 * n * Vector3.Dot(i, n));
                    float avgSpeed = (n_nocross.magnitude + j.magnitude) / (float)(((MovementPacket)player.PacketHistory.First.Value).timeCreated - ((MovementPacket)player.PacketHistory.First.Next.Next.Value).timeCreated).TotalSeconds;
                    Vector3 playerposition = ((MovementPacket)player.PacketHistory.First.Value).position + (PredictionPoint.normalized * avgSpeed * Mathf.Clamp(((float)((DateTime.Now) - player.PacketHistory.First.Value.timeCreated).TotalSeconds), -1, .75f));
                    player.Dummy.transform.position = playerposition;
                    player.Dummy.transform.rotation = ((MovementPacket)player.PacketHistory.First.Value).lookrotation * Quaternion.Euler((((MovementPacket)player.PacketHistory.First.Next.Value).lookrotation.eulerAngles - ((MovementPacket)player.PacketHistory.First.Value).lookrotation.eulerAngles));
                }
                else if (player.PacketHistory.Count > 0)
                {
                    Vector3 playerposition = ((MovementPacket)player.PacketHistory.First.Value).position;
                    player.Dummy.transform.position = playerposition;
                    player.Dummy.transform.rotation = ((MovementPacket)player.PacketHistory.First.Value).lookrotation;
                }
            }
        }
    }
}
