﻿using System.Collections;
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
        command = new byte[0];
    }
    public MovementPacket(Transform t, int num) : base(num)
    {
        position = t.position;
        lookrotation = t.rotation;
        command = new byte[0];
    }
    public MovementPacket(Vector3 p,Quaternion r, int num) : base(num)
    {
        position = p;
        lookrotation = r;
        command = new byte[0];
    }
    public override string id { get { return base.id + "-m"; } }
    public Vector3 position 
    {
        set { 
            xPos = value.x;
            yPos = value.y;
            zPos = value.z;
        }
        get { return new Vector3(xPos, yPos, zPos); }
    }
    public Quaternion lookrotation 
    {
        set
        {
            xRot = value.x;
            yRot = value.y;
            zRot = value.z;
            wRot = value.w;
        }
        get { return new Quaternion(xRot, yRot, zRot, wRot); }
    }
    private float xPos;
    private float yPos;
    private float zPos;
    private float xRot;
    private float yRot;
    private float zRot;
    private float wRot;
    
    public byte[] command;
}
