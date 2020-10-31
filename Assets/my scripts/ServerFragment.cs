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
    public Quaternion Rotation;
    public Vector3 position;
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
