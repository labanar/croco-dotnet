﻿namespace Croco.Core.Packets;

public readonly ref struct DeviceGetSerialPacket() : ICrocoPayloadlessPacket<DeviceGetSerialPacket, DeviceGetSerialResponse>
{
    public static byte CommandId => 253;
    public static DeviceGetSerialResponse Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        stream.ReadExactly(buffer);
        Span<char> serialBuffer = stackalloc char[buffer.Length * 2];
        for (int i = 0; i < buffer.Length; i++)
            buffer[i].TryFormat(serialBuffer.Slice(i * 2, 2), out _, "X2");

        return new(new(serialBuffer));
    }
}

public readonly ref struct DeviceGetSerialResponse(string serial)
{
    public readonly string Serial { get; } = serial;
}
