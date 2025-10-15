using ButtplugIo.GalakuDevice;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ButtplugIo
{
    public class DeviceEngine
    {
        private static DeviceManager selectedDevice;
        private static readonly GalakuDevice.BluetoothManager GalakuManager = new GalakuDevice.BluetoothManager();

        private static int threadBoolValue;
        private static string deviceSerialization;

        static DeviceEngine()
        {
            Program.AddConsoleInputKeyAction((k) =>
            {
                GalakuManager.ExecuteInputKey(k);
            });
            Program.AddGlobalInputKeyAction((k, b) =>
            {
                GalakuManager.ExecuteGlobalKey(k, b);
            });
            Task.Run(() =>
            {
                while (true)
                {
                    if (threadBoolValue == 1)
                    {
                        var deviceInfo = GetDeviceInfo();
                        if (deviceInfo != null && SendDeviceAdded(deviceInfo))
                        {
                            threadBoolValue = 2;
                        }
                    }
                    if (threadBoolValue == 2)
                    {
                        return;
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        private static bool SendDeviceAdded(DeviceInfo deviceInfo)
        {
            return Program.WebSocketSendText(JsonConvert.SerializeObject(new object[]
            {
                new
                {
                    DeviceAdded = new
                    {
                        Id = 0,
                        DeviceIndex = 0,
                        deviceInfo.DeviceName,
                        deviceInfo.DeviceMessages,
                    }
                }
            }), true).Result;
        }

        private static DeviceManager GetDeviceManager()
        {
            var lastInfo = selectedDevice?.GetDeviceInfo();
            if (lastInfo != null)
            {
                return selectedDevice;
            }
            if (GalakuManager.GetDeviceInfo() != null)
            {
                selectedDevice = GalakuManager;
                return selectedDevice;
            }
            return selectedDevice = null;
        }

        public static void SetScanningState(bool start)
        {
            threadBoolValue = threadBoolValue == 0 && start ? 1 : 2;
        }

        public static DeviceInfo GetDeviceInfo()
        {
            var deviceInfo = GetDeviceManager()?.GetDeviceInfo();
            if (deviceInfo != null)
            {
                var tostring = JsonConvert.SerializeObject(deviceInfo);
                if (tostring != deviceSerialization)
                {
                    try
                    {
                        File.WriteAllText("DeviceRecordNextTime.json", tostring);
                        deviceSerialization = tostring;
                    } catch { }
                }
            }
            else
            {
                try
                {
                    // 从缓存中加载上一次的设备信息
                    // 如需虚拟化设备，可以访问 
                    deviceSerialization = File.ReadAllText("DeviceRecordNextTime.json");
                    deviceInfo = JsonConvert.DeserializeObject<DeviceInfo>(deviceSerialization);
                } catch { }
            }
            return deviceInfo;
        }

        public static void ResolveCommandData(string name, JObject data)
        {
            GetDeviceManager()?.ExecuteCommand(name, data);
        }

        public static void StopDevice()
        {
            GetDeviceManager()?.StopDevice();
        }
    }

    public class DeviceInfo
    {
        public string DeviceName;
        public object DeviceMessages;
    }
}
