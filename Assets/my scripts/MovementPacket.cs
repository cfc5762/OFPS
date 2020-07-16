using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MovementPacket : Packet
{
    public MovementPacket(Transform t) : base() 
    {
        position = t.position;
        lookrotation = t.rotation;
        command = "";
    }
    public MovementPacket(Transform t,int num, IPEndPoint ep) : base(num,ep)
    {
        position = t.position;
        lookrotation = t.rotation;
        command = "";
    }
    public MovementPacket(Transform t, int num) : base(num)
    {
        position = t.position;
        lookrotation = t.rotation;
        command = "";
    }
    public MovementPacket(Vector3 p,Quaternion r, int num) : base(num)
    {
        position = p;
        lookrotation = r;
        command = "";
    }
    public override string id { get { return base.id + "-m"; } }
    public Vector3 position;
    public Quaternion lookrotation;
    public string command;
}
