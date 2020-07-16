using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;

public class HitPacket : MovementPacket
{
    public HitPacket(Transform camera, Transform body, int[] playernums) : base(body)
    {
        normal = camera.forward;
        hits = playernums;
        CameraLocation = camera.position;
    }
    public HitPacket(Transform camera, Transform body, int[] playernums,int playernum,IPEndPoint ep) : base(body,playernum,ep)
    {
        normal = camera.forward;
        hits = playernums;
        CameraLocation = camera.position;
    }
    public override string id {get{ char[] id = base.id.ToCharArray();
            id[id.Length-1]='h';
            return id.ArrayToString();
        }}
    public Vector3 normal;
    public int[] hits;
    public Vector3 CameraLocation;
}
