using System;
using System.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
[Serializable]
public class ServerFragment
{
    public DateTime timeCreated {
        get 
        {
            return new DateTime(ticks);
        }
        set 
        {
            ticks = value.Ticks;
        }
    }
    public long ticks;
    public Int16 playernum;
    public Int16 delay;
    public Vector3 position
    {
        set
        {
            xPos = value.x;
            yPos = value.y;
            zPos = value.z;
        }
        get { return new Vector3(xPos, yPos, zPos); }
    }
    public Quaternion Rotation
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

    public Int16 damageTaken;
    public byte[] toBytes() 
    {
        BinaryFormatter b = new BinaryFormatter();
        MemoryStream stream = new MemoryStream();
        b.Serialize(stream, this);
        return stream.GetBuffer();
    }
    public static bool FromBytes(byte[] bytes,out ServerFragment fragment) 
    {
        ServerFragment frag = new ServerFragment();
        bool nah = false;
        BinaryFormatter b = new BinaryFormatter();
        MemoryStream stream = new MemoryStream(bytes);
        try
        {
           var f = b.Deserialize(stream);
            frag = (ServerFragment)f;
        }
        catch (System.Exception)
        {

            nah = true;
        }
        
        if (!nah) 
        {
            fragment = frag;
            return true;
        }
        fragment = null;
        return false;
    }
}
