using System;
using System.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
[Serializable]
public class Packet
{
    public DateTime timeCreated
    {
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
    public virtual string id { get { return timeCreated.ToString() +"-"+playernum.ToString(); } }
    public Int16 playernum;
    public Packet() 
    {
        timeCreated = DateTime.Now;
        playernum = 1000;
    }
    public Packet(int num)
    {
        timeCreated = DateTime.Now;
        playernum = (short)num;
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
                return false;
            }
            return true;
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
