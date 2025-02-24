using System.Diagnostics;
using System.Text;

public class RomFileInfo
{
    public const int BANK_SIZE = 16384;
    public const int CHUNK_SIZE = 32;
    public const int CHUNKS_PER_BANK = BANK_SIZE / CHUNK_SIZE;
    private readonly byte[] _romData;

    public int Banks { get; }
    public string Name { get; }
    public bool IsGBC { get; }
    public int SpeedChangeBank { get; } = 0xffff;
    public ReadOnlySpan<byte> RomData => _romData;

    public RomFileInfo(string path)
    {
        _romData = File.ReadAllBytes(path);
        Banks = 1 << (_romData[0x0148] + 1);
        Debug.Assert(_romData.Length / BANK_SIZE == Banks);

        var nameSlice = _romData.AsSpan().Slice(0x134);
        var nameTerminator = nameSlice.IndexOf((byte)'\0');
        if (nameTerminator <= 0) nameTerminator = 16;
        var nameSpan = nameSlice.Slice(0, nameTerminator);
        Name = Encoding.UTF8.GetString(nameSpan);
        IsGBC = _romData[0x143] == 0xC0 || (_romData[0x143] == 0x80);

        if (IsGBC)
        {
            var speedchangeState = 0;
            for (var i = 0; (i < _romData.Length) && (SpeedChangeBank == 0xffff); i++)
            {
                var inst = _romData[i];
                switch (speedchangeState)
                {
                    case 0 when inst == 0xe0:
                        speedchangeState = 1;
                        break;
                    case 0:
                        break;
                    case 1 when inst == 0x4d:
                        speedchangeState = 2;
                        break;
                    case 1:
                        speedchangeState = 0;
                        break;
                    case 2 when inst == 0x10:
                        speedchangeState = 4;
                        SpeedChangeBank = (ushort)Math.Floor(i * 1.0m / BANK_SIZE);
                        return;
                    case 2 when inst == 0xc9:
                    case 2 when inst == 0xFF:
                        speedchangeState = 0;
                        break;
                    case 2 when inst == 0xe0:
                        speedchangeState = 3;
                        break;
                    case 2:
                        break;
                    case 3:
                        speedchangeState = 2;
                        break;
                    default:
                        Console.WriteLine("Weird invalid state here");
                        break;
                }
            }
        }
    }
}
