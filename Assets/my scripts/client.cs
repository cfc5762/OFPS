using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
    public static IPEndPoint server;
    public static string username;
    Thread recieveThread;
    public Socket socket;
    public List<Player> Players = new List<Player>();
    // Start is called before the first frame update
    void Awake()
    {
        
        if (instance != null && instance != this)
        {
            gameObject.SetActive(false);
        }
        else
        {
            
            unConfirmed = new LinkedList<HitPacket>();
            socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            playing = true;
            instance = this;
            recieveThread = new Thread(() => { Recieve(); });
            recieveThread.Start();
            DontDestroyOnLoad(gameObject);
        }
    }
    void Recieve() 
    {
        byte[] b = new byte[1024];
        EndPoint player = new IPEndPoint(IPAddress.Any, 0);
        socket.ReceiveFrom(b, ref player);
        PacketHandler.instance.OffloadClient(b, (IPEndPoint)player);
        if (playing)
            Recieve();
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
                socket.SendTo((new ConnectionPacket(username)).toBytes(), server);
            }
            yield return new WaitForSeconds(3);
        }    
    }
    IEnumerator MovementTick()
    {
        while (playing) 
        {
            MovementPacket movement = new MovementPacket(FpsController.transform, lastConnectionPacket.playernum);
            LinkedListNode<HitPacket> shot = unConfirmed.Last;
            for (int i = 0; i < unConfirmed.Count; i++)
            {
                socket.SendTo(shot.Value.toBytes(), server);
                if (shot.Previous == null)
                    break;
                shot = shot.Previous;
            }
            socket.SendTo(movement.toBytes(), server);
            yield return new WaitForSeconds(1f / 120f);
        }
    }
    void Recieve(Socket s) 
    {
        byte[] b = new byte[1024];
        EndPoint player = new IPEndPoint(IPAddress.Any, 0);
        s.ReceiveFrom(b, ref player);
        PacketHandler.instance.Offload(b, (IPEndPoint)player);
        if (playing)
            Recieve(s);
    }
    private void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(0)) 
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
