namespace Croco.Core.Packets;

public readonly ref struct RomRtcResponse
{
    public int RomId { get; }
    public readonly byte[] Data { get; }
    public RomRtcResponse(int romId, byte[] data)
    {
        RomId = romId;
        Data = data;
    }
}

public readonly ref struct RomGetRtcPacket(int romId) : ICrocoPacket<RomGetRtcPacket, RomRtcResponse>
{
    public static byte CommandId => 10;
    public int RomId => romId;

    public static RomRtcResponse Read(Stream stream)
    {
        var romId = stream.ReadByte();
        Span<byte> buffer = stackalloc byte[48];
        stream.ReadExactly(buffer);
        var response = new RomRtcResponse(romId, buffer.ToArray());
        return response;
    }

    public static void Write(RomGetRtcPacket packet, Stream stream)
    {
        stream.Write([CommandId, (byte)packet.RomId]);
    }
}
