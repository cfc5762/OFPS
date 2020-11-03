using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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
        server = new IPEndPoint(GetLocalIPAddress(), 7777);
        
        if (instance != null && instance != this)
        {
            gameObject.SetActive(false);
        }
        else
        {   
            unConfirmed = new LinkedList<HitPacket>();
            socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram, ProtocolType.Udp);
            
            playing = true;
            instance = this;
            StartCoroutine(ConnectionTick());
            StartCoroutine(MovementTick());
            Recieve();
            
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
                else
                {
                    unconfirmed = unconfirmed.Next;
                }
            }
            client.instance.socket.SendTo(P.toBytes(), client.server);

        }
    }
    public void clientHandlePacket(ServerFragment s)
    {
        while (s.playernum >= client.instance.Players.Count) //their playernum is higher than our max players
        {
            PacketHandler.makeNewPlayerClient(s);
        }
        MovementPacket m = new MovementPacket(s.position, s.Rotation, s.playernum);//movement packet of the enemy
        m.timeCreated = s.timeCreated;//make sure they are identical
        LinkedListNode<Packet> node;
        PacketHandler.placeInOrder(client.instance.Players[s.playernum].PacketHistory, m, out node);
        client.instance.Players[s.playernum].playernum = s.playernum;
        Enemy enemy = client.instance.Players[s.playernum].Dummy.GetComponent<Enemy>();//adjust the Enemy component
        enemy.username = client.instance.Players[s.playernum].userName;
        enemy.health = 100 - s.damageTaken;


    }
    IEnumerator ConnectionTick() 
    {
        while (playing) 
        {
            if (lastConnectionPacket != null)
            {


                Task.Run(() => socket.SendTo((new ConnectionPacket(username, lastConnectionPacket.playernum)).toBytes(), server));
            }
            else 
            {
                EndPoint e = new IPEndPoint(GetLocalIPAddress(), 7777);
                Task.Run(() => socket.SendTo((new ConnectionPacket(username, lastConnectionPacket.playernum)).toBytes(), e));
                
                
            }
            yield return new WaitForSecondsRealtime(3);
        }    
    }
    IEnumerator MovementTick()
    {
        while (playing && lastConnectionPacket != null) 
        {
            Task.Run(() =>
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
                socket.SendTo(movement.toBytes(), server);
            });

            
            yield return new WaitForSecondsRealtime(1f / 120f);
        }
    }
    void Recieve()
    {
        localEp = (IPEndPoint)socket.LocalEndPoint;
        socket.ReceiveTimeout = (1000);
        socket.Blocking = false;
        Task.Run(() =>
        {
            byte[] b = new byte[1024];
            EndPoint wanderingGamer = new IPEndPoint(IPAddress.Any, 0);
            while (playing)
            {
                if (socket.Available>0)
                {
                    socket.ReceiveFrom(b, ref wanderingGamer);
                    PacketHandler.instance.OffloadClient(b, (IPEndPoint)wanderingGamer);
                }                
            }
        });
        
        
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
                    // this graph explains this block https://gyazo.com/f242ed66b95169f5cf85347ccee5f671
                    Vector3 n_nocross = (((MovementPacket)player.PacketHistory.First.Value).position - ((MovementPacket)player.PacketHistory.First.Next.Value).position);
                    Vector3 n = Vector3.Cross(n_nocross, Vector3.up);
                    Vector3 i = (((MovementPacket)player.PacketHistory.First.Value).position - ((MovementPacket)player.PacketHistory.First.Next.Next.Value).position);
                    Vector3 j = (((MovementPacket)player.PacketHistory.First.Next.Value).position - ((MovementPacket)player.PacketHistory.First.Next.Next.Value).position);
                    Vector3 PredictionPoint = (i - 2 * n * Vector3.Dot(i, n));
                    float avgSpeed = (n_nocross.magnitude + j.magnitude) / (float)(((MovementPacket)player.PacketHistory.First.Value).timeCreated - ((MovementPacket)player.PacketHistory.First.Next.Next.Value).timeCreated).TotalSeconds;//place the enemy players with magic
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
