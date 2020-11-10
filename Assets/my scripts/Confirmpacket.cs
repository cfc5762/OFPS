using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfirmPacket : MovementPacket
{
    public ConfirmPacket(Transform t) : base(t)
    {
        
    }
    public ConfirmPacket(Transform t, int num) : base(t,num)
    {
       
    }
    public ConfirmPacket(Vector3 p, Quaternion r, int num) : base(p, r, num)
    {
       
    }
}
