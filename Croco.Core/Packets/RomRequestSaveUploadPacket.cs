namespace Croco.Core.Packets;

public readonly ref struct RomRequestUploadSavePacket(int romId) : ICrocoActionPacket<RomRequestUploadSavePacket>
{
    public static byte CommandId => 8;
    public readonly int RomId => romId;

    public static void Write(RomRequestUploadSavePacket packet, Stream stream)
    {
        stream.Write([CommandId, (byte)packet.RomId]);
    }
}
