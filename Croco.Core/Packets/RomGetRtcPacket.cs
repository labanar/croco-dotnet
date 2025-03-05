namespace Croco.Core.Packets;

public readonly ref struct RomRtcResponse(int romId, RtcData data)
{
    public readonly int RomId { get; } = romId;
    public readonly RtcData Data { get; } = data;
}

public readonly ref struct RomGetRtcPacket(int romId) : ICrocoPacket<RomGetRtcPacket, RomRtcResponse>
{
    public static byte CommandId => 10;
    public int RomId => romId;

    public static RomRtcResponse Read(Stream stream)
    {
        var romId = stream.ReadByte();
        var rtcData = new RtcData();
        stream.ReadExactly(rtcData);
        var response = new RomRtcResponse(romId, rtcData);
        return response;
    }

    public static void Write(RomGetRtcPacket packet, Stream stream)
    {
        stream.Write([CommandId, (byte)packet.RomId]);
    }
}
