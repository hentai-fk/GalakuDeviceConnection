using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace ButtplugIo.GalakuDevice
{
    internal class BleControl
    {
        public static readonly Guid WRITE_SERVICE_UUID = new Guid("00001000-0000-1000-8000-00805f9b34fb");
        public static readonly Guid WRITE_CHARACTERISTIC_UUID = new Guid("00001001-0000-1000-8000-00805f9b34fb");
        public readonly BTDeviceInfo deviceInfo;
        private BluetoothLEDevice bleDevice;
        private GattCharacteristic charaWrite;


        private byte[] sendBuffer = new byte[10];


        private BleControl(BTDeviceInfo deviceInfo) { this.deviceInfo = deviceInfo; }

        public static async Task<BleControl> CreateBleControl(BTDeviceInfo deviceInfo, BluetoothLEDevice bleDevice)
        {
            var gattServicesResult = await bleDevice?.GetGattServicesForUuidAsync(WRITE_SERVICE_UUID);
            if (gattServicesResult.Status == GattCommunicationStatus.Success)
            {
                var serviceWrite = gattServicesResult.Services[0];
                var gattCharacteristicsResult = await serviceWrite?.GetCharacteristicsForUuidAsync(WRITE_CHARACTERISTIC_UUID);
                if (gattCharacteristicsResult?.Status == GattCommunicationStatus.Success)
                {
                    return new BleControl(deviceInfo)
                    {
                        bleDevice = bleDevice,
                        charaWrite = gattCharacteristicsResult.Characteristics[0],
                    };
                }
            }
            return null;
        }

        public bool IsConnected()
        {
            return deviceInfo != null && bleDevice?.ConnectionStatus == BluetoothConnectionStatus.Connected && charaWrite != null;
        }

        public void Close()
        {
            try { charaWrite?.Service?.Dispose(); } catch { }
            try { bleDevice?.Dispose(); } catch { }
            bleDevice = null;
            charaWrite = null;
        }

        public void SendControl()
        {
            if (!deviceInfo.IsDataChanged)
                return;
            if (deviceInfo.DeviceTypeInt == (int)DeviceType.DeviceShakeTwo)
            {
                sendIntensityTwo(Math.Max(0, Math.Min(100, deviceInfo.MadaValueA)));
            }
        }

        public void SendHotLevel(int level)
        {
            sendDataToDevice(getHotBytes(level));
        }

        private void sendIntensityTwo(int i)
        {
            if (deviceInfo.LocalName == "G149")
            {
                sendDataToDevice(getIntensityBytesTwoG149(i));
            }
            else if (deviceInfo.LocalName == "G145")
            {
                sendDataToDevice(getIntensityBytesTwoG145(i));
            }
            else
            {
                sendDataToDevice(getIntensityBytesTwo(i));
            }
        }

        private async void sendDataToDevice(byte[] bArr)
        {
            if (charaWrite != null)
            {
                var writer = new DataWriter();
                writer.WriteBytes(bArr);
                try
                {
                    await charaWrite?.WriteValueAsync(writer.DetachBuffer());
                }
                catch { }
            }
        }


        private byte[] getIntensityBytesTwo(int var1)
        {
            this.resetSendBytes();
            BTDeviceInfo var7 = deviceInfo;
            byte var2;
            byte var3;
            double var4;
            if (var7.MadaEnableA && var7.MadaEnableB)
            {
                if (var7.IsStrong)
                {
                    var4 = (double)var1;
                    if (var4 < 0.05)
                    {
                        var2 = (byte)0;
                    }
                    else
                    {
                        var2 = (byte)((int)(var4 * 0.01 * 60.0 + 40.0));
                    }
                }
                else
                {
                    var2 = (byte)var1;
                }

                var3 = (byte)var1;
            }
            else if (var7.MadaEnableA)
            {
                do {
                    int var6 = var1;
                    if (var7.IsStrong)
                    {
                        var4 = (double)var1;
                        if (var4 < 0.05)
                        {
                            var2 = (byte)0;
                            break;
                        }

                        var6 = (int)(var4 * 0.01 * 60.0 + 40.0);
                    }

                    var2 = (byte)var6;
                } while (false);

                var3 = 0;
            }
            else if (var7.MadaEnableB)
            {
                var3 = (byte)var1;
                var2 = 0;
            }
            else
            {
                var3 = 0;
                var2 = 0;
            }

            byte[] var8 = this.sendBuffer;
            var8[0] = 90;
            var8[1] = 0;
            var8[2] = 0;
            var8[3] = 1;
            var8[4] = 64;
            var8[5] = 3;
            var8[6] = var2;
            var8[7] = var3;
            var8[8] = 0;
            var8[9] = 0;
            return EnDesCommand.SendBytes(var8, 10);
        }

        private byte[] getIntensityBytesTwoG145(int var1)
        {
            this.resetSendBytes();
            BTDeviceInfo var5 = deviceInfo;
            int var4;
            if (var1 >= 30)
            {
                var4 = (int)((double)var1 * 0.01 * 70.0 + 30.0);
            }
            else
            {
                var4 = 0;
            }

            byte var2;
            byte var3;
            if (var5.MadaEnableA && var5.MadaEnableB)
            {
                var3 = (byte)var1;
                var2 = (byte)var4;
            }
            else if (var5.MadaEnableA)
            {
                var3 = (byte)var1;
                var2 = 0;
            }
            else if (var5.MadaEnableB)
            {
                var2 = (byte)var4;
                var3 = 0;
            }
            else
            {
                var3 = 0;
                var2 = 0;
            }

            byte[] var6 = this.sendBuffer;
            var6[0] = 90;
            var6[1] = 0;
            var6[2] = 0;
            var6[3] = 1;
            var6[4] = 64;
            var6[5] = 3;
            var6[6] = var3;
            var6[7] = var2;
            var6[8] = 0;
            var6[9] = 0;
            return EnDesCommand.SendBytes(var6, 10);
        }

        private byte[] getIntensityBytesTwoG149(int var1)
        {
            this.resetSendBytes();
            BTDeviceInfo var5 = deviceInfo;
            int var4;
            if (var1 > 0)
            {
                var4 = (int)((double)((float)var1) / 6.6 + 15.0);
                var1 = (int)((double)var1 * 0.01 * 70.0 + 30.0);
            }
            else
            {
                var4 = var1;
            }

            byte var2;
            byte var3;
            if (var5.MadaEnableA && var5.MadaEnableB)
            {
                var3 = (byte)var1;
                var2 = (byte)var4;
            }
            else if (var5.MadaEnableA)
            {
                var3 = (byte)var1;
                var2 = 0;
            }
            else if (var5.MadaEnableB)
            {
                var2 = (byte)var4;
                var3 = 0;
            }
            else
            {
                var3 = 0;
                var2 = 0;
            }

            byte[] var6 = this.sendBuffer;
            var6[0] = 90;
            var6[1] = 0;
            var6[2] = 0;
            var6[3] = 1;
            var6[4] = 64;
            var6[5] = 3;
            var6[6] = var3;
            var6[7] = var2;
            var6[8] = 0;
            var6[9] = 0;
            return EnDesCommand.SendBytes(var6, 10);
        }

        private byte[] getHotBytes(int i)
        {
            resetSendBytes();
            byte[] bArr = this.sendBuffer;
            bArr[0] = 90;
            bArr[1] = 0;
            bArr[2] = 0;
            bArr[3] = 1;
            bArr[4] = unchecked((byte)SByte.MinValue);
            bArr[5] = (byte)i;
            bArr[6] = 0;
            bArr[7] = 0;
            bArr[8] = 0;
            bArr[9] = 0;
            return EnDesCommand.SendBytes(bArr, 10);
        }


        private void resetSendBytes()
        {
            for (int i = 0; i < sendBuffer.Length; i++)
            {
                sendBuffer[i] = 0;
            }
        }
    }
}
