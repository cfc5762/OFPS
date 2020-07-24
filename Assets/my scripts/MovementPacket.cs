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
        _position = new Tuple<float, float, float>(t.position.x, t.position.y, t.position.z);
        _lookrotation = new Tuple<float, float, float, float>(t.rotation.x, t.rotation.y, t.rotation.z,t.rotation.w);
        command = "";
    }
    public MovementPacket(Transform t,int num, IPEndPoint ep) : base(num,ep)
    {
        _position = new Tuple<float, float, float>(t.position.x, t.position.y, t.position.z);
        _lookrotation = new Tuple<float, float, float, float>(t.rotation.x, t.rotation.y, t.rotation.z, t.rotation.w);
        command = "";
    }
    public MovementPacket(Transform t, int num) : base(num)
    {
        _position = new Tuple<float, float, float>(t.position.x, t.position.y, t.position.z);
        _lookrotation = new Tuple<float, float, float, float>(t.rotation.x, t.rotation.y, t.rotation.z, t.rotation.w);
        command = "";
    }
    public MovementPacket(Vector3 p,Quaternion r, int num) : base(num)
    {
        _position = new Tuple<float, float, float>(p.x, p.y, p.z);
        _lookrotation = new Tuple<float, float, float, float>(r.x, r.y, r.z, r.w);
        command = "";
    }
    public override string id { get { return base.id + "-m"; } }
    public Tuple<float,float,float>_position;
    public Vector3 position 
    {
        get{ return new Vector3(_position.Item1, _position.Item2, _position.Item3); } 
    }
    public Tuple<float, float, float, float> _lookrotation;
    public Quaternion lookrotation
    {
        get { return new Quaternion(_lookrotation.Item1, _lookrotation.Item2, _lookrotation.Item3, _lookrotation.Item4); }
    }
    public string command;
}
