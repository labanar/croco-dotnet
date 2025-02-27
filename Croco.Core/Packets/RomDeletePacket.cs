namespace Croco.Core.Packets;

public readonly ref struct RomDeletePacket : ICrocoActionPacket<RomDeletePacket>
{
    public static byte CommandId => 5;

    public readonly int RomId { get; }

    public RomDeletePacket(int romId)
    {
        RomId = romId;
    }

    public static void Write(RomDeletePacket packet, Stream stream) =>
        stream.Write([CommandId, (byte)packet.RomId]);
}
