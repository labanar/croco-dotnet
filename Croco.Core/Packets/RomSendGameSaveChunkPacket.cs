﻿using System.Buffers.Binary;

namespace Croco.Core.Packets;

public readonly ref struct RomSendGameSaveChunkPacket : ICrocoPacket<RomSendGameSaveChunkPacket, ActionResponse>
{
    public static byte CommandId => 9;
    public readonly ushort Bank { get; }
    public readonly ushort Chunk { get; }
    public readonly ReadOnlySpan<byte> Data { get; }

    public RomSendGameSaveChunkPacket(ushort bank, ushort chunk, ReadOnlySpan<byte> data)
    {
        Bank = bank;
        Chunk = chunk;
        Data = data;
    }

    public static ActionResponse Read(Stream stream)
    {
        Span<byte> output = stackalloc byte[1];
        stream.ReadExactly(output);
        return new ActionResponse(output[0]);
    }

    public static void Write(RomSendGameSaveChunkPacket packet, Stream stream)
    {
        Span<byte> headers = stackalloc byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(headers, packet.Bank);
        BinaryPrimitives.WriteUInt16BigEndian(headers.Slice(2), packet.Chunk);
        stream.Write([CommandId, .. headers, .. packet.Data]);
    }
}
