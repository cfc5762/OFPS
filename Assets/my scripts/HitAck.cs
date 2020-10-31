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
    }
    public HitAck(bool h) : base() 
    {
        hit = h;
    }
    public HitAck(bool h,int num) : base(num)
    {
        hit = h;
    }
    public override string id { get { return base.id + "-h"; } }
    bool hit;
}
