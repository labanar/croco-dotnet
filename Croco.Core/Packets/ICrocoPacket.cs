namespace Croco.Core.Packets;

public interface ICrocoPacket<TRequest, TResponse>
    where TRequest : ICrocoPacket<TRequest, TResponse>, allows ref struct
    where TResponse : allows ref struct
{
    static abstract byte CommandId { get; }
    static abstract void Write(TRequest packet, Stream stream);
    static abstract TResponse Read(Stream stream);
}

public interface ICrocoActionPacket<TPak> : ICrocoPacket<TPak, ActionResponse>
    where TPak : ICrocoPacket<TPak, ActionResponse>, allows ref struct
{
    static ActionResponse ICrocoPacket<TPak, ActionResponse>.Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[1];
        stream.ReadExactly(buffer);
        return new ActionResponse(buffer[0]);
    }
}

public interface ICrocoPayloadlessPacket<TPak, TResponse> : ICrocoPacket<TPak, TResponse>
    where TPak : ICrocoPacket<TPak, TResponse>, allows ref struct
    where TResponse : allows ref struct
{
    static void ICrocoPacket<TPak, TResponse>.Write(TPak packet, Stream stream)
    {
        stream.Write([TPak.CommandId]);
    }
}


public readonly ref struct ActionResponse
{
    public bool Success { get; }
    public int ErrorCode { get; }
    public ActionResponse(byte result)
    {
        if (result == 0)
        {
            Success = true;
            ErrorCode = 0;
        }
        else
        {
            Success = false;
            ErrorCode = result;
        }
    }
}
