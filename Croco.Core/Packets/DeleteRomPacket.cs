namespace Croco.Core.Packets;

public readonly ref struct DeleteRomPacket : ICrocoActionPacket<DeleteRomPacket>
{
    public static byte CommandId => 5;

    public readonly int RomId { get; }

    public DeleteRomPacket(int romId)
    {
        RomId = romId;
    }

    public static void Write(DeleteRomPacket packet, Stream stream) =>
        stream.Write([CommandId, (byte)packet.RomId]);
}
