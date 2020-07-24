using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
[Serializable]
public class ServerFragment
{
    public DateTime timeCreated;
    public int playernum;
    public int delay;
    public Tuple<float,float,float>_position;
    public Vector3 position 
    {
        get{ return new Vector3(_position.Item1, _position.Item2, _position.Item3); }
        set { _position = new Tuple<float, float, float>(value.x, value.y, value.z); }
    }
    public Tuple<float, float, float, float> _lookrotation;
    public Quaternion Rotation
    {
        get { return new Quaternion(_lookrotation.Item1, _lookrotation.Item2, _lookrotation.Item3, _lookrotation.Item4); }
        set { _lookrotation = new Tuple<float, float, float, float>(value.x, value.y, value.z, value.w); }
    }
    public int damageTaken;
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
