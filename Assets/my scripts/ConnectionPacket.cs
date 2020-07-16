using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public class ConnectionPacket : Packet
{
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
