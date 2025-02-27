using System.Buffers.Binary;
using System.Text;

namespace Croco.Core.Packets;

public readonly ref struct RomGetInfoPacket(int romId) :
    ICrocoPacket<RomGetInfoPacket, RomGetInfoResponse>,
    ICrocoPacket<RomGetInfoPacket, RomGetInfoWithMbcResponse>
{
    public static byte CommandId => 4;
    public readonly int RomId { get; } = romId;
    public static void Write(RomGetInfoPacket packet, Stream stream)
    {
        stream.Write([CommandId, (byte)packet.RomId]);
    }

    static RomGetInfoWithMbcResponse ICrocoPacket<RomGetInfoPacket, RomGetInfoWithMbcResponse>.Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[21];
        stream.ReadExactly(buffer);
        var name = ReadName(buffer);
        var numRamBanks = (int)buffer[17];
        byte mbc = (byte)buffer[18];
        var numRomBanks = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(19));
        return new RomGetInfoWithMbcResponse(name, numRamBanks, numRomBanks, mbc);
    }

    static RomGetInfoResponse ICrocoPacket<RomGetInfoPacket, RomGetInfoResponse>.Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[20];
        stream.ReadExactly(buffer);
        var name = ReadName(buffer);
        var numRamBanks = (int)buffer[17];
        var numRomBanks = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(18));
        return new RomGetInfoResponse(name, numRamBanks, numRomBanks);
    }

    private static string ReadName(ReadOnlySpan<byte> buffer)
    {
        var nameTerminator = buffer.IndexOf((byte)'\0');
        if (nameTerminator <= 0) nameTerminator = 16;
        var nameSpan = buffer.Slice(0, nameTerminator);
        return Encoding.UTF8.GetString(nameSpan);
    }
}

public readonly ref struct RomGetInfoWithMbcResponse
{
    public readonly string Name { get; }
    public readonly int NumRamBanks { get; }
    public readonly int NumRomBanks { get; }
    public readonly byte Mbc { get; }

    public RomGetInfoWithMbcResponse(string name, int numRamBanks, int numRomBanks, byte mbc)
    {
        Name = name;
        NumRamBanks = numRamBanks;
        NumRomBanks = numRomBanks;
        Mbc = mbc;
    }

}

public readonly ref struct RomGetInfoResponse
{
    public readonly ReadOnlySpan<char> Name { get; }
    public readonly int NumRamBanks { get; }
    public readonly int NumRomBanks { get; }

    public RomGetInfoResponse(ReadOnlySpan<char> name, int numRamBanks, int numRomBanks)
    {
        Name = name;
        NumRamBanks = numRamBanks;
        NumRomBanks = numRomBanks;
    }
}

