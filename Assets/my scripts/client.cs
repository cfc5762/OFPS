using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class client : MonoBehaviour
{
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
        
        username = "silktail";
        server = new IPEndPoint(GetLocalIPAddress(), 28960);
        localEp = new IPEndPoint(GetLocalIPAddress(), 28960);
        if (instance != null && instance != this)
        {
            gameObject.SetActive(false);
        }
        else
        {   
            unConfirmed = new LinkedList<HitPacket>();
            socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(localEp);
            playing = true;

            StartCoroutine(ConnectionTick());
            StartCoroutine(MovementTick());


            instance = this;
            //Recieve();
             recieveThread = new Thread(() => { Recieve(); });
             recieveThread.Start();
            DontDestroyOnLoad(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        playing = false;
    }
    private void OnApplicationQuit()
    {
        playing = false;
    }
    IEnumerator ConnectionTick() 
    {
        while (playing) 
        {
            if (lastConnectionPacket != null)
            {
                socket.SendTo((new ConnectionPacket(username, lastConnectionPacket.playernum)).toBytes(), server);
            }
            else 
            {
                EndPoint e = new IPEndPoint(GetLocalIPAddress(), 28960);
                socket.SendTo((new ConnectionPacket(username)).toBytes(), e);
            }
            yield return new WaitForSeconds(3);
        }    
    }
    IEnumerator MovementTick()
    {
        while (playing && lastConnectionPacket != null) 
        {
            MovementPacket movement = new MovementPacket(FpsController.transform, lastConnectionPacket.playernum);//construct a movement packet out of our player
            LinkedListNode<HitPacket> shot = unConfirmed.Last;
            for (int i = 0; i < unConfirmed.Count; i++)//send all unconfirmed shots
            {
                socket.SendTo(shot.Value.toBytes(), server);
                if (shot.Previous == null)
                    break;
                shot = shot.Previous;
            }
            socket.SendTo(movement.toBytes(), server);//send movement packet
            yield return new WaitForSeconds(1f / 120f);
        }
    }
    void Recieve()
    {
        while (playing)
        {
            byte[] b = new byte[1024];
            EndPoint wanderingGamer = new IPEndPoint(IPAddress.Any, 0);
            socket.ReceiveFrom(b, ref wanderingGamer);
            PacketHandler.instance.OffloadClient(b, (IPEndPoint)wanderingGamer);
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
