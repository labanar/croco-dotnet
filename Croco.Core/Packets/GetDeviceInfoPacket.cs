namespace Croco.Core.Packets
{
    public readonly ref struct GetDeviceInfoPacket : ICrocoPayloadlessPacket<GetDeviceInfoPacket, DeviceInfoResponse>
    {
        public static byte CommandId => 254;
        public static DeviceInfoResponse Read(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[11];
            stream.ReadExactly(buffer);

            var featureStep = (int)buffer[0];
            var hwVersion = (int)buffer[1];
            var major = (int)buffer[2];
            var minor = (int)buffer[3];
            var patch = (int)buffer[4];
            var supportsMbcInfo = featureStep >= 3;
            var supportsSpeedChangeBankInfo = featureStep >= 2;

            return new DeviceInfoResponse(hwVersion, major, minor, patch, supportsMbcInfo, supportsSpeedChangeBankInfo);
        }
    }

    public readonly ref struct DeviceInfoResponse
    {
        public readonly int HardwareVersion { get; }
        public readonly int FirmwareVersion_Major { get; }
        public readonly int FirmwareVersion_Minor { get; }
        public readonly int FirmwareVersion_Patch { get; }
        public bool SupportsMbcInfo { get; }
        public bool SupportsSpeedChangeBankInfo { get; }

        public DeviceInfoResponse(int hardwareVersion, int firmwareVersion_Major, int firmwareVersion_Minor, int firmwareVersion_Patch, bool supportsMbc, bool supportsSpeedChangeBankInfo)
        {
            HardwareVersion = hardwareVersion;
            FirmwareVersion_Major = firmwareVersion_Major;
            FirmwareVersion_Minor = firmwareVersion_Minor;
            FirmwareVersion_Patch = firmwareVersion_Patch;
            SupportsMbcInfo = supportsMbc;
            SupportsSpeedChangeBankInfo = supportsSpeedChangeBankInfo;
        }

        public DeviceInfoResponse(Span<byte> hwInfo)
        {
            var featureStep = (int)hwInfo[0];
            HardwareVersion = (int)hwInfo[1];
            var major = (int)hwInfo[2];
            var minor = (int)hwInfo[3];
            var patch = (int)hwInfo[4];
            SupportsMbcInfo = featureStep >= 3;
            SupportsSpeedChangeBankInfo = featureStep >= 2;
        }
    }
}
