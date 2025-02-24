//using System.Buffers.Binary;
//using System.Text;

//namespace Croco.Core;

//public static class CrocoCartridgeExtensions
//{
//    public static string GetSerial(this CrocoCartridge cart)
//    {
//        Span<byte> buffer = stackalloc byte[8];
//        cart.Connection.SendCommand(253, [], buffer);
//        Span<char> serialBuffer = stackalloc char[buffer.Length * 2];
//        for (int i = 0; i < buffer.Length; i++)
//        {
//            buffer[i].TryFormat(serialBuffer.Slice(i * 2, 2), out _, "X2");
//        }

//        return new string(serialBuffer);
//    }

//    public static IEnumerable<RomInfo> GetInstalledRoms(this CrocoCartridge cart)
//    {
//        var romsInfo = cart.GetRomsInfo();
//        for (int i = 0; i < romsInfo.NumRoms; i++)
//        {
//            yield return cart.GetRomInfo(i);
//        }
//    }

//    public static RomsInfo GetRomsInfo(this CrocoCartridge cart)
//    {
//        Span<byte> buffer = stackalloc byte[5];
//        cart.Connection.SendCommand(1, [], buffer);

//        var numRoms = (int)buffer[0];
//        var usedBanks = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(1, 2));
//        var maxBanks = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(3, 2));
//        return new(numRoms, usedBanks, maxBanks);
//    }


//    public static RomInfo GetRomInfo(this CrocoCartridge cart, int romId)
//    {
//        Span<byte> payload = [(byte)romId];
//        Span<byte> buffer = stackalloc byte[cart.SupportsMbcInfo ? 21 : 20];
//        cart.Connection.SendCommand(4, payload, buffer);

//        var nameTerminator = buffer.IndexOf((byte)'\0');
//        if (nameTerminator <= 0) nameTerminator = 16;

//        var nameSpan = buffer.Slice(0, nameTerminator);
//        var name = Encoding.UTF8.GetString(nameSpan);
//        var numRamBanks = (int)buffer[17];
//        byte mbc = cart.SupportsMbcInfo ? (byte)buffer[18] : (byte)0xFF;
//        var numRomBanks = 0;
//        if (buffer.Length > 19)
//            numRomBanks = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(19));

//        return new RomInfo(romId, name, numRamBanks, numRomBanks, mbc);
//    }

//    public static bool RequestRomUpload(this CrocoCartridge cart, int banks, string name, int speedChangeBank)
//    {
//        Span<byte> payload = stackalloc byte[cart.SupportsSpeedChangeBankInfo ? 21 : 19];
//        Span<byte> buffer = stackalloc byte[1];

//        BinaryPrimitives.WriteUInt16BigEndian(payload.Slice(0, 2), (ushort)banks);
//        payload.Slice(2).Fill(0);
//        Encoding.UTF8.TryGetBytes(name, payload.Slice(2, 16), out _);
//        if (cart.SupportsSpeedChangeBankInfo)
//            payload[19] = (byte)speedChangeBank;

//        cart.Connection.SendCommand(2, payload, buffer);
//        if (buffer[0] == 1) return false;

//        if (buffer[0] != 0)
//        {
//            throw new Exception("Failed to request rom upload - Code: " + buffer[0]);
//        }

//        return true;
//    }

//    public static void SendRomChunk(this CrocoCartridge cart, int bank, int chunk, ReadOnlySpan<byte> data)
//    {
//        Span<byte> headers = stackalloc byte[4];
//        BinaryPrimitives.WriteUInt16BigEndian(headers, (ushort)bank);
//        BinaryPrimitives.WriteUInt16BigEndian(headers.Slice(2), (ushort)chunk);

//        Span<byte> buffer = stackalloc byte[1];
//        ReadOnlySpan<byte> payload = [.. headers, .. data];
//        cart.Connection.SendCommand(3, payload, buffer);

//        if (buffer[0] != 0)
//        {
//            throw new Exception("Failed to upload rom chunk - Code: " + buffer[0]);
//        }
//    }

//    public static void DeleteRom(this CrocoCartridge cart, int romId)
//    {
//        Span<byte> payload = [(byte)romId];
//        Span<byte> buffer = stackalloc byte[1];
//        cart.Connection.SendCommand(5, payload, buffer);
//        if (buffer[0] != 0)
//        {
//            throw new Exception("Failed to delete rom");
//        }
//    }
//}
