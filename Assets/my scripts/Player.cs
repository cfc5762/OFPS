using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Player 
{
    public Player() 
    {
        userName = "";
        Delay = 0;
        connected = false;
        damageTaken = 0;
        playernum = 0;
        respawnTimer = 0;
        PacketHistory = new LinkedList<Packet>();
        HealthHistory = new LinkedList<int>();
        EndPoint = new IPEndPoint(IPAddress.Any,0);
        Dummy = null;
    }
    public string userName;
    public bool connected;
    public int playernum;
    public int respawnTimer;
    public LinkedList<int> HealthHistory;
    public LinkedList<Packet> PacketHistory;
    public IPEndPoint EndPoint;
    public GameObject Dummy;
    public int damageTaken;
    public float Delay;
    public static bool operator ==(Player a, Player b)
    {
        return (a.EndPoint.Address == b.EndPoint.Address && a.EndPoint.Port == b.EndPoint.Port);
    }
    public static bool operator !=(Player a, Player b)
    {
        return (a.EndPoint.Address != b.EndPoint.Address || a.EndPoint.Port != b.EndPoint.Port);
    }
    public static bool operator ==(Player a, IPEndPoint b)
    {
        return (a.EndPoint.Address == b.Address && a.EndPoint.Port == b.Port);
    }
    public static bool operator !=(Player a, IPEndPoint b)
    {
        return (a.EndPoint.Address != b.Address || a.EndPoint.Port != b.Port);
    }
}
