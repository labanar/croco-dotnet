using Croco.Core.Packets;
using System.Buffers.Binary;

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
        var deviceInfo = Connection.SendPacket<DeviceGetInfoPacket, DeviceGetInfoResponse>(new());

        HardwareVersion = deviceInfo.HardwareVersion;
        FirmwareVersion = new(deviceInfo.FirmwareVersion_Major, deviceInfo.FirmwareVersion_Minor, deviceInfo.FirmwareVersion_Patch);
        SupportsMbcInfo = deviceInfo.SupportsMbcInfo;
        SupportsSpeedChangeBankInfo = deviceInfo.SupportsSpeedChangeBankInfo;

        var serial = Connection.SendPacket<DeviceGetSerialPacket, DeviceGetSerialResponse>(new());
        SerialNumber = serial.Serial;
    }

    public void UploadRom(string filePath)
    {
        var romFile = new RomFileInfo(filePath);

        Console.WriteLine("Requesting rom upload...");
        var romUploadPak = new RomRequestUploadPacket((ushort)romFile.Banks, romFile.Name, SupportsSpeedChangeBankInfo ? (ushort)romFile.SpeedChangeBank : null);
        var romUploadResponse = Connection.SendPacket(romUploadPak);


        Console.WriteLine("Uploading chunks...");
        if (romUploadResponse.Success)
        {
            for (ushort bank = 0; bank < romFile.Banks; bank++)
            {
                for (ushort chunk = 0; chunk < CrocoConstants.CHUNKS_PER_BANK; chunk++)
                {
                    var offset = (bank * CrocoConstants.BANK_SIZE) + (chunk * CrocoConstants.CHUNK_SIZE);
                    var chunkSpan = romFile.RomData.Slice(offset, CrocoConstants.CHUNK_SIZE);
                    Connection.SendPacket(new RomSendChunkPacket(bank, chunk, chunkSpan));
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

    public void DownloadRomSave(int romId, string outputFilePath)
    {
        var romInfo = GetRomInfo(romId);
        var bytesToTransfer = romInfo.Mbc != 2 ? romInfo.RamBanks * CrocoConstants.RAM_BANK_SIZE : CrocoConstants.MBC2_RAM_SIZE;
        var bytesTransferred = 0;
        var hasRtcData = false;



        Span<byte> rtcResponse = stackalloc byte[50];
        Connection.SendPacketRaw<RomGetRtcPacket, RomRtcResponse>(new(romId), rtcResponse);
        if (rtcResponse[0] == 255)
        {
            Console.WriteLine("No RTC Data");
        }
        else
        {
            hasRtcData = true;
        }

        var saveFileSize = hasRtcData ? bytesToTransfer + CrocoConstants.RTC_SAVE_SIZE : bytesToTransfer;


        var rtcData = hasRtcData ? rtcResponse.Slice(2, CrocoConstants.RTC_SAVE_SIZE) : [];

        //Write to a temp file then do a copy/overwrite of the output file path
        using var fs = File.OpenWrite(outputFilePath);

        //Request save game
        var response = Connection.SendPacket<RomRequestSavePacket, RomRequestSaveResponse>(new RomRequestSavePacket((ushort)romId));

        while (bytesTransferred < bytesToTransfer)
        {
            var bank = (int)Math.Floor(1.0m * bytesTransferred / CrocoConstants.RAM_BANK_SIZE);
            var chunk = (int)(bytesTransferred - (bank * CrocoConstants.RAM_BANK_SIZE)) / CrocoConstants.CHUNK_SIZE;

            //Console.WriteLine("Expecting bank: {0} chunk: {1} {2}/{3} Bytes", bank, chunk, bytesTransferred, bytesToTransfer);

            var gameSaveChunk = Connection.SendPacket<RomReceiveGameSaveChunkPacket, RomReceiveGameSaveChunkResponse>(new());
            bytesTransferred += CrocoConstants.CHUNK_SIZE;
            fs.Write([.. gameSaveChunk.Data]);
        }

        //If we have RTC data we need to place it at the end of the save file
        if (hasRtcData)
        {
            //RTC data is 48 bytes, we skip the first 40 and take the last 8 to get the timestamp, not sure what's in the first 40 bytes
            var localTimestamp = BinaryPrimitives.ReadUInt64LittleEndian(rtcData.Slice(40));
            //Console.WriteLine("RTC Timestamp: {0}", localTimestamp);

            var localDate = DateTimeOffset.FromUnixTimeSeconds((long)localTimestamp);
            localDate = DateTime.SpecifyKind(localDate.DateTime, DateTimeKind.Local);

            var tz = TimeZoneInfo.Local;
            var utcDate = localDate + tz.BaseUtcOffset;

            var utcTimestamp = utcDate.ToUnixTimeSeconds();
            BinaryPrimitives.WriteUInt64LittleEndian(rtcData.Slice(40), (ulong)utcTimestamp);
            fs.Write(rtcData);
        }

        fs.Flush();
    }


    public void UploadRomSave(int romId, string saveFilePath)
    {
        var romInfo = GetRomInfo(romId);
        var bytesToTransfer = romInfo.Mbc != 2 ? romInfo.RamBanks * CrocoConstants.RAM_BANK_SIZE : CrocoConstants.MBC2_RAM_SIZE;

        var saveData = File.ReadAllBytes(saveFilePath);

        var hasRtcData = saveData.Length == bytesToTransfer + CrocoConstants.RTC_SAVE_SIZE;

        //Split out the RTC data from the save game data

        var response = Connection.SendPacket(new RomRequestUploadSavePacket(romId));
        var bytesTransferred = 0;
        while (bytesTransferred < bytesToTransfer)
        {
            var bank = (ushort)Math.Floor(1.0m * bytesTransferred / CrocoConstants.RAM_BANK_SIZE);
            var chunk = (ushort)((bytesTransferred - (bank * CrocoConstants.RAM_BANK_SIZE)) / CrocoConstants.CHUNK_SIZE);
            //Console.WriteLine("Expecting bank: {0} chunk: {1} {2}/{3} Bytes", bank, chunk, bytesTransferred, bytesToTransfer);

            var data = saveData.AsSpan().Slice(bytesTransferred, CrocoConstants.CHUNK_SIZE);
            Connection.SendPacket<RomSendGameSaveChunkPacket>(new(bank, chunk, data));
            bytesTransferred += CrocoConstants.CHUNK_SIZE;
        }

        if (hasRtcData)
        {
            var rtcBytes = saveData.AsSpan().Slice(bytesToTransfer, CrocoConstants.RTC_SAVE_SIZE);

            var utcTimestamp = BinaryPrimitives.ReadUInt64LittleEndian(rtcBytes.Slice(40));
            //Console.WriteLine("RTC Timestamp: {0}", utcTimestamp);

            var utcDate = DateTimeOffset.FromUnixTimeSeconds((long)utcTimestamp);

            var tz = TimeZoneInfo.Local;
            var localDate = utcDate - tz.BaseUtcOffset;

            var localTimestamp = localDate.ToUnixTimeSeconds();
            BinaryPrimitives.WriteUInt64LittleEndian(rtcBytes.Slice(40), (ulong)localTimestamp);

            var rtcData = new RtcData();
            rtcBytes.CopyTo(rtcData);
            Connection.SendPacket<RomSendRtcPacket>(new(romId, rtcData));
        }
    }


    public void DeleteRom(int romId)
    {
        Connection.SendPacket(new RomDeletePacket(romId));

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
        var romRequest = new RomGetInfoPacket(romId);

        if (SupportsMbcInfo)
        {
            var mbcRomResponse = Connection.SendPacket<RomGetInfoPacket, RomGetInfoWithMbcResponse>(romRequest);
            return new(romId, mbcRomResponse.Name, mbcRomResponse.NumRamBanks, mbcRomResponse.NumRomBanks, mbcRomResponse.Mbc);
        }

        var romResponse = Connection.SendPacket<RomGetInfoPacket, RomGetInfoResponse>(romRequest);
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


