using Croco.Core.Packets;

namespace Croco.Core;

public class CrocoCartridge : IAsyncDisposable
{
    public ICrocoConnection Connection { get; }
    public bool SupportsSpeedChangeBankInfo { get; }
    public bool SupportsMbcInfo { get; }
    public string SerialNumber { get; }
    public int HardwareVersion { get; }
    public Version FirmwareVersion { get; }

    public CrocoCartridge(ICrocoConnection connection)
    {
        Connection = connection;
        var deviceInfo = Connection.SendPacket<GetDeviceInfoPacket, DeviceInfoResponse>(new());

        HardwareVersion = deviceInfo.HardwareVersion;
        FirmwareVersion = new(deviceInfo.FirmwareVersion_Major, deviceInfo.FirmwareVersion_Minor, deviceInfo.FirmwareVersion_Patch);
        SupportsMbcInfo = deviceInfo.SupportsMbcInfo;
        SupportsSpeedChangeBankInfo = deviceInfo.SupportsSpeedChangeBankInfo;

        var serial = Connection.SendPacket<GetSerialPacket, GetSerialResponse>(new());
        SerialNumber = serial.Serial;
    }

    public void UploadRom(string filePath)
    {
        var romFile = new RomFileInfo(filePath);

        Console.WriteLine("Requesting rom upload...");
        var romUploadPak = new RequestUploadRomPacket((ushort)romFile.Banks, romFile.Name, SupportsSpeedChangeBankInfo ? (ushort)romFile.SpeedChangeBank : null);
        var romUploadResponse = Connection.SendPacket(romUploadPak);


        Console.WriteLine("Uploading chunks...");
        if (romUploadResponse.Success)
        {
            for (ushort bank = 0; bank < romFile.Banks; bank++)
            {
                for (ushort chunk = 0; chunk < RomFileInfo.CHUNKS_PER_BANK; chunk++)
                {
                    var offset = (bank * RomFileInfo.BANK_SIZE) + (chunk * RomFileInfo.CHUNK_SIZE);
                    var chunkSpan = romFile.RomData.Slice(offset, RomFileInfo.CHUNK_SIZE);
                    Connection.SendPacket(new SendRomChunkPacket(bank, chunk, chunkSpan));
                }
                Console.WriteLine($"Uploaded bank {bank + 1}/{romFile.Banks}");
            }
        }
        else
        {
            if (romUploadResponse.ErrorCode == 1)
            {
                Console.WriteLine("This ROM is already installed on the cartridge.");
                return;
            }

            Console.WriteLine($"Can't upload this ROM at the moment (Error Code: {romUploadResponse.ErrorCode}).");
        }
    }

    public void DeleteRom(int romId)
    {
        Connection.SendPacket(new DeleteRomPacket(romId));

        //If you do not refresh the roms list the cartridge enters a weird state where it stops responding after a delete request.
        foreach (var _ in GetRoms()) { }
    }

    public RomsInfo GetRomsInfo()
    {
        var pak = new GetRomUtilizationPacket();
        var response = Connection.SendPacket<GetRomUtilizationPacket, GetRomUtilizationResponse>(pak);
        return new(response.NumRoms, response.UsedBanks, response.MaxBanks);
    }

    public RomInfo GetRomInfo(int romId)
    {
        var romRequest = new GetRomInfoPacket(romId);

        if (SupportsMbcInfo)
        {
            var mbcRomResponse = Connection.SendPacket<GetRomInfoPacket, GetRomInfoResponseWithMbc>(romRequest);
            return new(romId, mbcRomResponse.Name, mbcRomResponse.NumRamBanks, mbcRomResponse.NumRomBanks, mbcRomResponse.Mbc);
        }

        var romResponse = Connection.SendPacket<GetRomInfoPacket, GetRomInfoResponse>(romRequest);
        return new(romId, romResponse.Name.ToString(), romResponse.NumRamBanks, romResponse.NumRomBanks, 0xFF);
    }

    public IEnumerable<RomInfo> GetRoms()
    {
        var romsInfo = GetRomsInfo();
        for (int i = 0; i < romsInfo.NumRoms; i++)
            yield return GetRomInfo(i);
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}


public readonly record struct RomInfo(int RomId, string Name, int RamBanks, int RomBanks, byte Mbc)
{
    public string GetCartridgeTypeDescription => Mbc switch
    {
        0x00 => "ROM ONLY",
        0x01 => "MBC1",
        0x02 => "MBC1+RAM",
        0x03 => "MBC1+RAM+BATTERY",
        0x05 => "MBC2",
        0x06 => "MBC2+BATTERY",
        0x08 => "ROM+RAM 9",
        0x09 => "ROM+RAM+BATTERY 9",
        0x0B => "MMM01",
        0x0C => "MMM01+RAM",
        0x0D => "MMM01+RAM+BATTERY",
        0x0F => "MBC3+TIMER+BATTERY",
        0x10 => "MBC3+TIMER+RAM+BATTERY 10",
        0x11 => "MBC3",
        0x12 => "MBC3+RAM 10",
        0x13 => "MBC3+RAM+BATTERY 10",
        0x19 => "MBC5",
        0x1A => "MBC5+RAM",
        0x1B => "MBC5+RAM+BATTERY",
        0x1C => "MBC5+RUMBLE",
        0x1D => "MBC5+RUMBLE+RAM",
        0x1E => "MBC5+RUMBLE+RAM+BATTERY",
        0x20 => "MBC6",
        0x22 => "MBC7+SENSOR+RUMBLE+RAM+BATTERY",
        0xFC => "POCKET CAMERA",
        0xFD => "BANDAI TAMA5",
        0xFE => "HuC3",
        0xFF => "HuC1+RAM+BATTERY",
        _ => "Unknown"
    };
}

public readonly record struct RomsInfo(int NumRoms, int UsedBanks, int MaxBanks);


