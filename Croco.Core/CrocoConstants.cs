namespace Croco.Core;

internal static class CrocoConstants
{
    public const int BANK_SIZE = 16384;
    public const int CHUNK_SIZE = 32;
    public const int CHUNKS_PER_BANK = BANK_SIZE / CHUNK_SIZE;



    public const int RAM_BANK_SIZE = 0x2000;
    public const int MBC2_RAM_SIZE = 0x200;
    public const int RTC_SAVE_SIZE = 48;
}
