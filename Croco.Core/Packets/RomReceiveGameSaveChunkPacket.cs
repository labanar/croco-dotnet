using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Croco.Core.Packets;

public readonly ref struct RomReceiveGameSaveChunkPacket : ICrocoPayloadlessPacket<RomReceiveGameSaveChunkPacket, RomReceiveGameSaveChunkResponse>
{
    public static byte CommandId => 7;

    public static RomReceiveGameSaveChunkResponse Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[36];
        stream.ReadExactly(buffer);
        var bank = BinaryPrimitives.ReadUInt16BigEndian(buffer);
        var chunk = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2));
        var chunkData = new ChunkData();
        buffer.Slice(4, 32).CopyTo(chunkData);
        return new RomReceiveGameSaveChunkResponse(bank, chunk, chunkData);
    }
}

public readonly ref struct RomReceiveGameSaveChunkResponse
{
    public readonly int Bank { get; }
    public readonly int Chunk { get; }
    public readonly ChunkData Data { get; }
    public RomReceiveGameSaveChunkResponse(int bank, int chunk, ChunkData data)
    {
        Bank = bank;
        Chunk = chunk;
        Data = data;
    }
}


[InlineArray(CrocoConstants.CHUNK_SIZE)]
public struct ChunkData
{
    private byte _element0;
}
