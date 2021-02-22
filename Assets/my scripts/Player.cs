
using Steamworks;
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
        SteamID = 0;
        Dummy = null;
    }
    public string userName;
    public bool connected;
    public int playernum;
    public int respawnTimer;
    public LinkedList<int> HealthHistory;
    public LinkedList<Packet> PacketHistory;
    public SteamId SteamID;
    public GameObject Dummy;
    public int damageTaken;
    public float Delay;
    public static bool operator ==(Player a, Player b)
    {
        return (a.SteamID == b.SteamID);
    }
    public static bool operator !=(Player a, Player b)
    {
        return (a.SteamID != b.SteamID);
    }
}
