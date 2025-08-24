using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;

[DataContract]
public struct BoardEntry
{
    [DataMember] public Position Pos;
    [DataMember] public ChessPiece Piece;
}

[DataContract]
public class BoardLayoutMessage
{
    [DataMember]
    public List<BoardEntry> Board { get; set; }

    [DataMember]
    public List<ChessPiece> CapturedWhite { get; set; }

    [DataMember]
    public List<ChessPiece> CapturedBlack { get; set; }

    [DataMember]
    public bool WhiteTurn { get; set; }
}

[DataContract]
public struct MoveMessage
{
    [DataMember]
    public Position From { get; set; }

    [DataMember]
    public Position To { get; set; }
}

public static class MessageSerializer
{
    public static string ToJson<T>(T obj)
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        using (var ms = new MemoryStream())
        {
            serializer.WriteObject(ms, obj);
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }

    public static T FromJson<T>(string json)
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
        {
            return (T)serializer.ReadObject(ms);
        }
    }

    public static byte[] ToBinary<T>(T obj)
    {
        using (var ms = new MemoryStream())
        {
            var bf = new BinaryFormatter();
#pragma warning disable SYSLIB0011
            bf.Serialize(ms, obj);
#pragma warning restore SYSLIB0011
            return ms.ToArray();
        }
    }

    public static T FromBinary<T>(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        {
            var bf = new BinaryFormatter();
#pragma warning disable SYSLIB0011
            return (T)bf.Deserialize(ms);
#pragma warning restore SYSLIB0011
        }
    }
}
