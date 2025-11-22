using System;
using System.Buffers.Binary;

public struct NetMessage
{
    public uint Tick;
    public byte[] Payload;

    public static byte[] Encode(NetMessage message)
    {
        byte[] payload = message.Payload ?? Array.Empty<byte>();
        if (payload.Length > ushort.MaxValue)
        {
            throw new ArgumentException("Payload too large for message encoding.");
        }

        byte[] buffer = new byte[sizeof(uint) + sizeof(ushort) + payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, message.Tick);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(sizeof(uint)), (ushort)payload.Length);
        payload.AsSpan().CopyTo(buffer.AsSpan(sizeof(uint) + sizeof(ushort)));
        return buffer;
    }

    public static NetMessage Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < sizeof(uint) + sizeof(ushort))
        {
            throw new ArgumentException("Message data too short to decode header.");
        }

        uint tick = BinaryPrimitives.ReadUInt32LittleEndian(data);
        ushort payloadLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(sizeof(uint)));

        if (data.Length < sizeof(uint) + sizeof(ushort) + payloadLength)
        {
            throw new ArgumentException("Message payload length exceeds provided data.");
        }

        byte[] payload = new byte[payloadLength];
        data.Slice(sizeof(uint) + sizeof(ushort), payloadLength).CopyTo(payload);

        return new NetMessage
        {
            Tick = tick,
            Payload = payload
        };
    }
}
