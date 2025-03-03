using System.Buffers.Binary;

namespace Croco.Core.Packets;

internal class RomReceiveGameSaveChunkPacket : ICrocoPayloadlessPacket<RomReceiveGameSaveChunkPacket, RomReceiveGameSaveChunkResponse>
{
    public static byte CommandId => 7;

    public static RomReceiveGameSaveChunkResponse Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[36];
        stream.ReadExactly(buffer);
        var bank = BinaryPrimitives.ReadUInt16BigEndian(buffer);
        var chunk = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2));
        var data = buffer.Slice(4);
        return new RomReceiveGameSaveChunkResponse(bank, chunk, data.ToArray());
    }
}

public readonly ref struct RomReceiveGameSaveChunkResponse
{
    public readonly ushort Bank { get; }
    public readonly ushort Chunk { get; }
    public readonly byte[] Data { get; }

    public RomReceiveGameSaveChunkResponse(ushort bank, ushort chunk, byte[] data)
    {
        Bank = bank;
        Chunk = chunk;
        Data = data;
    }
}
