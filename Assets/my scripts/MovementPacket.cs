using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System;
[Serializable]
public class MovementPacket : Packet
{
    public MovementPacket(Transform t) : base() 
    {
        position = t.position;
        lookrotation = t.rotation;
        command = 0;
    }
    public MovementPacket(Transform t, int num) : base(num)
    {
        position = t.position;
        lookrotation = t.rotation;
        command = 0;
    }
    public MovementPacket(Vector3 p,Quaternion r, int num) : base(num)
    {
        position = p;
        lookrotation = r;
        command = 0;
    }
    public override string id { get { return base.id + "-m"; } }
    public Vector3 position;
    public Quaternion lookrotation;
    public byte command;
}
