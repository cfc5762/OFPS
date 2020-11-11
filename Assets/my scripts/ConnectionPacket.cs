using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class ConnectionPacket : Packet
{
    public ConnectionPacket() : base()
    {
        ticks = 0;
        playernum = 0;
        usernames = new string[0];
        username = "";
    }
    public string[] usernames;
    public ConnectionPacket(string u, int pn) : base(pn) 
    {
        username = u;
    }
    public ConnectionPacket(string u): base()
    {
        username = u;
    }
    public override string id {get{ return base.id + username; }}
    public string username;
}
