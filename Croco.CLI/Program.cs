using Croco.Core;
using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;

if (!LibUsbCrocoConnection.TryConnectToCartridge(out var connection) || connection == null)
{
    Console.WriteLine("Failed to connect to cartridge.");
    return;
}

Cli.Ext.ConfigureServices(services =>
{
    services.AddSingleton(new CrocoCartridge(connection));
});

Cli.Run<CrocoCommand>();


[CliCommand(Description = "A command utility to interface with a Croco Cartridge")]
public class CrocoCommand
{
    [CliCommand(Description = "Retrieve information about the cartridge")]
    public class Info(CrocoCartridge cartridge)
    {
        public void Run()
        {
            Console.WriteLine($"Serial: {cartridge.SerialNumber}");
            Console.WriteLine($"HW version: {cartridge.HardwareVersion}");
            Console.WriteLine($"FW version: {cartridge.FirmwareVersion}");
            var info = cartridge.GetRomsInfo();
            Console.WriteLine($"ROMs: {info.NumRoms}");
            Console.WriteLine($"Banks: {info.UsedBanks}/{info.MaxBanks} (Remaining: {info.MaxBanks - info.UsedBanks})");
        }
    }

    [CliCommand(Description = "Retrieve serial number of the cartridge")]
    public class Serial(CrocoCartridge cartridge)
    {
        public void Run()
        {
            Console.WriteLine(cartridge.SerialNumber);
        }
    }

    [CliCommand(Description = "Retrieve the hardware version of the cartridge")]
    public class HwVersion(CrocoCartridge cartridge)
    {
        public void Run()
        {
            Console.WriteLine(cartridge.HardwareVersion);
        }
    }

    [CliCommand(Description = "Retrieve the firmware version of the cartridge")]
    public class FwVersion(CrocoCartridge cartridge)
    {
        public void Run()
        {
            Console.WriteLine(cartridge.FirmwareVersion);
        }
    }
}



[CliCommand(Description = "ROM commands", Parent = typeof(CrocoCommand), Name = "rom")]
public class RomCommands
{

    [CliCommand(Description = "Retrieve the save game file for a ROM", Name = "get-save")]
    public class GetSaveGame(CrocoCartridge cartridge)
    {
        [CliArgument(Description = "RomId to retrieve save game for")]
        public int RomId { get; set; }

        [CliArgument(Description = "Save file path - must be a full file name, not a directory")]
        public required string SaveFilePath { get; set; }

        public void Run()
        {
            cartridge.DownloadRomSave(RomId, SaveFilePath);
        }
    }


    [CliCommand(Description = "Upload a game save to the cartridge", Name = "upload-save")]
    public class UploadSaveCommand(CrocoCartridge cartridge)
    {
        [CliArgument(Description = "RomId to retrieve save game for")]
        public int RomId { get; set; }

        [CliArgument(Description = "Save file path - must be a full file name, not a directory")]
        public required string SaveFilePath { get; set; }

        public void Run()
        {
            cartridge.UploadRomSave(RomId, SaveFilePath);
        }
    }


    [CliCommand(Description = "Retrieve details about what is stored on the cartridge and how much space is available.")]
    public class Utilization(CrocoCartridge cartridge)
    {
        public void Run()
        {
            var info = cartridge.GetRomsInfo();
            Console.WriteLine($"ROMs: {info.NumRoms}");
            Console.WriteLine($"Banks: {info.UsedBanks}/{info.MaxBanks} (Remaining: {info.MaxBanks - info.UsedBanks})");
        }
    }

    [CliCommand(Description = "List ROMs installed on the cartridge", Parent = typeof(RomCommands))]
    public class List(CrocoCartridge cartridge)
    {
        public void Run()
        {
            foreach (var romInfo in cartridge.GetRoms())
            {
                Console.WriteLine($"{romInfo.RomId}) {romInfo.Name} | Ram Banks: {romInfo.RamBanks} | Rom Banks: {romInfo.RomBanks} | Type: {romInfo.GetCartridgeTypeDescription}");
            }
        }
    }



    [CliCommand(Description = "Deletes a ROM from the cartridge by it's ID.", Parent = typeof(RomCommands))]
    public class Delete(CrocoCartridge cartridge)
    {
        [CliArgument(Description = "RomId to delete")]
        public int RomId { get; set; }

        public void Run()
        {
            RomInfo? info = null;

            foreach (var romInfo in cartridge.GetRoms())
            {
                if (romInfo.RomId == RomId)
                {
                    info = romInfo;
                    break;
                }
            }

            if (!info.HasValue)
            {
                Console.WriteLine($"Rom with id {RomId} not found");
                return;
            }

            Console.WriteLine($"Are you sure you want to delete {info.Value.Name} the following rom? (Y/N)");
            while (true)
            {
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Y)
                {
                    cartridge.DeleteRom(RomId);
                    Console.WriteLine();
                    Console.WriteLine($"Rom with id {RomId} deleted.");
                    break;
                }
                else if (key.Key == ConsoleKey.N)
                {
                    Console.WriteLine();
                    Console.WriteLine("Operation cancelled.");
                    break;
                }
            }
        }
    }

    [CliCommand(Description = "Upload a ROM to the cartridge", Parent = typeof(RomCommands))]
    public class Upload(CrocoCartridge cartridge)
    {
        [CliArgument(Description = "Path to the ROM file")]
        public required string Path { get; set; }

        public void Run()
        {
            cartridge.UploadRom(Path);
        }
    }
}


