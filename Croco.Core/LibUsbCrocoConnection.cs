using Croco.Core.Packets;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace Croco.Core
{
    public class LibUsbCrocoConnection : ICrocoConnection
    {
        private static readonly UsbDeviceFinder[] Finders = [new(0x2E8A, 0x107F), new(0xcafe, 0x2142)];
        private readonly UsbContext _context;
        private readonly IUsbDevice _device;
        private readonly UsbEndpointReader _reader;
        private readonly UsbEndpointWriter _writer;
        private readonly UsbStream _stream;

        private LibUsbCrocoConnection(UsbContext context, IUsbDevice device, UsbEndpointReader reader, UsbEndpointWriter writer)
        {
            _context = context;
            _device = device;
            _reader = reader;
            _writer = writer;
            _stream = new UsbStream(_writer, _reader);
        }

        public ActionResponse SendPacket<TPak>(TPak packet)
            where TPak : ICrocoPacket<TPak, ActionResponse>, allows ref struct
             => SendPacket<TPak, ActionResponse>(packet);

        public TResponse SendPacket<TPak, TResponse>(TPak packet)
            where TPak : ICrocoPacket<TPak, TResponse>, allows ref struct
            where TResponse : allows ref struct
        {
            TPak.Write(packet, _stream);

            var echoCommand = _stream.ReadByte();
            if (echoCommand != TPak.CommandId)
                throw new InvalidOperationException("Invalid response");

            return TPak.Read(_stream);
        }

        public static bool TryConnectToCartridge(out ICrocoConnection? connection)
        {
            var (context, device) = GetCrocoDevice();
            if (device == null)
            {
                context.Dispose();
                connection = null;
                return false;
            }

            var (commInterface, endpointIn, endpointOut) = GetCommunicationsInterface(device);

            if (commInterface == null || endpointIn == null || endpointOut == null)
            {
                connection = null;
                return false;
            }

            device.Open();
            device.ClaimInterface(commInterface.Number);
            device.SetAltInterface(0);
            UsbSetupPacket setupPacket = new UsbSetupPacket(
                              (byte)(UsbCtrlFlags.Direction_Out | UsbCtrlFlags.RequestType_Class | UsbCtrlFlags.Recipient_Interface),
                              0x22,         // request
                              0x01,         // value
                              (short)commInterface.Number, // index
                              0             // length
                          );

            var controlTransferREsult = device.ControlTransfer(setupPacket);

            WriteEndpointID writeEndpointID = (WriteEndpointID)endpointOut;
            ReadEndpointID readEndpointID = (ReadEndpointID)endpointIn;

            var reader = device.OpenEndpointReader(readEndpointID);
            var writer = device.OpenEndpointWriter(writeEndpointID);
            connection = new LibUsbCrocoConnection(context, device, reader, writer);
            return true;
        }


        private static (UsbContext context, IUsbDevice? device) GetCrocoDevice()
        {
            var context = new UsbContext();
            foreach (var finder in Finders)
            {
                var device = context.Find(finder);
                if (device != null)
                {
                    return (context, device);
                }
            }

            return (context, null);
        }

        private static (UsbInterfaceInfo? usbInterface, byte? endpointIn, byte? endpointOut) GetCommunicationsInterface(IUsbDevice device)
        {
            if (!TryGetCommunicationInterface(device, out var usbInterface) || usbInterface == null)
            {
                return (null, null, null);
            }


            byte? inEndpoint = null, outEndpoint = null;
            foreach (var endpoint in usbInterface.Endpoints)
            {
                var isIn = (endpoint.EndpointAddress & 0x80) == 0x80;
                if (isIn)
                {
                    inEndpoint = endpoint.EndpointAddress;
                }
                else
                {
                    outEndpoint = endpoint.EndpointAddress;
                }
            }

            return (usbInterface, inEndpoint, outEndpoint);

        }

        private static bool TryGetCommunicationInterface(IUsbDevice device, out UsbInterfaceInfo? usbInterface)
        {
            usbInterface = null;
            foreach (var i in device.Configs[0].Interfaces)
            {
                if (i.Class != LibUsbDotNet.ClassCode.VendorSpec) continue;
                usbInterface = i;
                return true;

            }
            return false;
        }

        public async ValueTask DisposeAsync()
        {
            await _stream.DisposeAsync();
            _device.Dispose();
            _context.Dispose();
        }
    }
}
