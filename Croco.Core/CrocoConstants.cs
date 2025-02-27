namespace Croco.Core;

internal static class CrocoConstants
{
    public const int BANK_SIZE = 16384;
    public const int CHUNK_SIZE = 32;
    public const int CHUNKS_PER_BANK = BANK_SIZE / CHUNK_SIZE;
}
