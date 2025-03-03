namespace Croco.Core.Packets;

public readonly ref struct RomRequestSaveResponse
{
    public int ErrorCode { get; init; }
}


public readonly ref struct RomRequestSavePacket : ICrocoPacket<RomRequestSavePacket, RomRequestSaveResponse>
{
    public static byte CommandId => 6;
    public readonly ushort RomId { get; }

    public RomRequestSavePacket(ushort romId)
    {
        RomId = romId;
    }

    public static void Write(RomRequestSavePacket packet, Stream stream)
    {
        stream.Write([CommandId, (byte)packet.RomId]);
    }

    public static RomRequestSaveResponse Read(Stream stream)
    {
        var expectedData = new byte[1];
        stream.ReadExactly(expectedData);

        return new RomRequestSaveResponse { ErrorCode = expectedData[0] };
    }
}
