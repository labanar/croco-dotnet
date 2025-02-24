using Croco.Core.Packets;

namespace Croco.Core;
public interface ICrocoConnection : IAsyncDisposable
{
    TResponse SendPacket<TPak, TResponse>(TPak packet)
            where TPak : ICrocoPacket<TPak, TResponse>, allows ref struct
            where TResponse : allows ref struct;

    ActionResponse SendPacket<TPak>(TPak packet)
        where TPak : ICrocoPacket<TPak, ActionResponse>, allows ref struct;
}
