using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class HitAck:Packet
{
    public HitAck(HitPacket p,bool h) : base(p.playernum) 
    {
        timeCreated = p.timeCreated;
        hit = h;
        command = p.command;
    }
    public HitAck(bool h) : base() 
    {
        hit = h;
        command = new byte[0];
    }
    public HitAck(bool h,int num) : base(num)
    {
        hit = h;
        command = new byte[0];
    }
    public override string id { get { return base.id + "-h"; } }
    public byte[] command;
    bool hit;
}
