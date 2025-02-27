using Croco.Core;
using System.Diagnostics;
using System.Text;

public class RomFileInfo
{
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

        var actualBanks = _romData.Length / CrocoConstants.BANK_SIZE;
        Debug.Assert(actualBanks == Banks, "ROM length does not match number of banks defined inside rom");

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
                        SpeedChangeBank = (ushort)Math.Floor(i * 1.0m / CrocoConstants.BANK_SIZE);
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
