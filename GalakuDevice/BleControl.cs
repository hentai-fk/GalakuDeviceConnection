using Newtonsoft.Json.Linq;
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
        private List<GattCharacteristic> charaNotify = new List<GattCharacteristic>();


        private object closeObjectLocker = new object();

        private byte batteryValue = 100;
        private byte[] sendBuffer = new byte[10];


        private BleControl(BTDeviceInfo deviceInfo) { this.deviceInfo = deviceInfo; }

        public static async Task<BleControl> CreateBleControl(BTDeviceInfo deviceInfo, BluetoothLEDevice bleDevice)
        {
            var servicesResult = await bleDevice?.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (servicesResult.Status == GattCommunicationStatus.Success)
            {
                var instance = new BleControl(deviceInfo) { bleDevice = bleDevice };
                foreach (var service in servicesResult.Services)
                {
                    var characteristicsResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (characteristicsResult.Status == GattCommunicationStatus.Success)
                    {
                        foreach (var characteristic in characteristicsResult.Characteristics)
                        {
                            if (instance.charaWrite == null && service.Uuid == WRITE_SERVICE_UUID && characteristic.Uuid == WRITE_CHARACTERISTIC_UUID)
                            {
                                instance.charaWrite = characteristic;
                                continue;
                            }
                            if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                            {
                                instance.charaNotify.Add(characteristic);
                            }
                        }
                    }
                }
                if (instance.charaWrite != null)
                {
                    // 接收通知
                    foreach (var item in instance.charaNotify)
                    {
                        var status = await item.WriteClientCharacteristicConfigurationDescriptorAsync(
                            GattClientCharacteristicConfigurationDescriptorValue.Notify
                        );
                        if (status == GattCommunicationStatus.Success)
                        {
                            item.ValueChanged += instance.OnCharacteristicChanged;
                        }
                    }
                    return instance;
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
            lock (closeObjectLocker)
            {
                try { bleDevice?.Dispose(); } catch { }
                bleDevice = null;
                charaWrite = null;
                charaNotify = null;
            }
        }

        private void OnCharacteristicChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var dataReader = DataReader.FromBuffer(args.CharacteristicValue);
            var data = new byte[dataReader.UnconsumedBufferLength];
            dataReader.ReadBytes(data);
            if (data.Length >= 12)
            {
                receiveBytes(EnDesCommand.Decrypt(data));
            }
        }

        public void SendControl()
        {
            if (!deviceInfo.IsDataChanged)
                return;
            deviceInfo.IsDataChanged = false;
            if (deviceInfo.DeviceTypeInt == (int)DeviceType.DeviceShakeTwo)
            {
                sendIntensityTwo(Math.Max(0, Math.Min(100, deviceInfo.MadaValueA)));
            }
        }

        public string GetBatteryText()
        {
            return $"{batteryValue}%";
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

        public void receiveBytes(byte[] bArr)
        {
            if (bArr.Count() >= 16)
            {
                byte b = bArr[2];
                byte b2 = bArr[3];
                if (b == 12)
                {
                    updateBattery(b2);
                }
            }
            else if (bArr.Count() >= 12)
            {
                byte b3 = bArr[2];
                if (b3 == unchecked((byte)-79))
                {
                    byte b4 = bArr[4];
                    byte b5 = bArr[6];
                    byte b6 = bArr[9];
                    updateBattery(b4);
                    updateLedByte(b6);
                    updateHotLevel(b5);
                }
            }
        }

        private void updateBattery(byte value)
        {
            batteryValue = value;
        }

        private void updateHotLevel(byte value)
        {
            if (deviceInfo.HotLevel == value)
            {
                SendHotLevel(deviceInfo.HotLevel);
            }
        }

        private void updateLedByte(byte value) { }

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
