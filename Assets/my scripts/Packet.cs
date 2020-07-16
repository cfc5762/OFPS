using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEditor.Sprites;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
[Serializable]
public class Packet
{
    public virtual string id { get { return timeCreated.ToString() +"-"+playernum.ToString(); } }
    public DateTime timeCreated;
    public IPEndPoint FromUser;
    public int playernum;
    public Packet() 
    {
        timeCreated = DateTime.Now;
        playernum = 1000;
        FromUser = new IPEndPoint(IPAddress.Any, 0);

    }
    public Packet(int num,IPEndPoint ep)
    {
        timeCreated = DateTime.Now;
        playernum = num;
        FromUser = ep;

    }
    public Packet(int num)
    {
        timeCreated = DateTime.Now;
        playernum = num;
        FromUser = new IPEndPoint(IPAddress.Any, 0);

    }
    public static bool operator ==(Packet x, Packet y)
    {
        if (y is Packet)
        {
            return (x.id == y.id);
        }
        else 
        {
            if (y is null && x is null) 
            {
                return true;
            }
            return false;
        }
    }
    public static bool operator !=(Packet x, Packet y)
    {
        if (y is Packet)
        {
            return (x.id != y.id);
        }
        else
        {
            if (y is null && x is null)
            {
                return true;
            }
            return false;
        }
    }
    public virtual byte[] toBytes()
    {
        BinaryFormatter b = new BinaryFormatter();
        MemoryStream stream = new MemoryStream();
        b.Serialize(stream, this);
        return stream.GetBuffer();
    }
    public static bool FromBytes(byte[] bytes, out Packet packet)
    {
        
        Packet pack = new Packet();
        bool nah = false;
        BinaryFormatter b = new BinaryFormatter();
        MemoryStream stream = new MemoryStream(bytes);
        try
        {
            var f = b.Deserialize(stream);
            pack = (Packet)f;
        }
        catch (System.Exception)
        {
            nah = true;
        }

        if (!nah)
        {
            packet = pack;
            return true;
        }
        packet = null;
        return false;
    }
}
