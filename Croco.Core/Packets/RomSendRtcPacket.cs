using System.Runtime.CompilerServices;

namespace Croco.Core.Packets;


public readonly ref struct RomSendRtcPacket(int romId, RtcData rtcData) : ICrocoActionPacket<RomSendRtcPacket>
{
    public static byte CommandId => 11;
    public readonly int RomId { get; } = romId;
    public readonly RtcData Data { get; } = rtcData;

    public static void Write(RomSendRtcPacket packet, Stream stream)
    {
        stream.Write([CommandId, (byte)packet.RomId, .. packet.Data]);
    }
}




[InlineArray(CrocoConstants.RTC_SAVE_SIZE)]
public struct RtcData
{
    private byte _element0;
}
