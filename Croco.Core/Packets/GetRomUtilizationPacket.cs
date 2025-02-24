using System.Buffers.Binary;

namespace Croco.Core.Packets;

public readonly ref struct GetRomUtilizationPacket() : ICrocoPayloadlessPacket<GetRomUtilizationPacket, GetRomUtilizationResponse>
{
    public static byte CommandId => 1;
    public static GetRomUtilizationResponse Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[5];
        stream.ReadExactly(buffer);
        var numRoms = (int)buffer[0];
        var banksUsed = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(1, 2));
        var maxBanks = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(3, 2));
        return new(numRoms, banksUsed, maxBanks);
    }
}

public readonly ref struct GetRomUtilizationResponse
{
    public readonly int NumRoms { get; }
    public readonly int UsedBanks { get; }
    public readonly int MaxBanks { get; }
    public GetRomUtilizationResponse(int numRoms, int usedBanks, int maxBanks)
    {
        NumRoms = numRoms;
        UsedBanks = usedBanks;
        MaxBanks = maxBanks;
    }
}
