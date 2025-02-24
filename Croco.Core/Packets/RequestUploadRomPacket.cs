
using System.Buffers.Binary;
using System.Text;

namespace Croco.Core.Packets;

public readonly ref struct RequestUploadRomPacket : ICrocoActionPacket<RequestUploadRomPacket>
{
    public static byte CommandId => 2;
    public readonly ushort Banks { get; }
    public readonly ReadOnlySpan<char> Name { get; }
    public readonly ushort? SpeedChangeBank { get; }

    public RequestUploadRomPacket(ushort banks, ReadOnlySpan<char> name, ushort? speedChangeBank)
    {
        Banks = banks;
        Name = name;
        SpeedChangeBank = speedChangeBank;
    }

    public static void Write(RequestUploadRomPacket packet, Stream stream)
    {
        Span<byte> payload = stackalloc byte[packet.SpeedChangeBank.HasValue ? 21 : 19];
        BinaryPrimitives.WriteUInt16BigEndian(payload.Slice(0, 2), packet.Banks);
        payload.Slice(2).Fill(0);
        Encoding.UTF8.TryGetBytes(packet.Name, payload.Slice(2, 16), out _);

        if (packet.SpeedChangeBank.HasValue)
            BinaryPrimitives.WriteUInt16BigEndian(payload.Slice(19, 2), packet.SpeedChangeBank.Value);

        stream.Write([CommandId, .. payload]);
    }
}
