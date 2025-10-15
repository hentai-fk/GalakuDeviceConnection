using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace ButtplugIo.GalakuDevice
{
    public class BluetoothManager : DeviceManager
    {
        private static readonly Dictionary<string, BTDeviceInfo> devices_list = new Dictionary<string, BTDeviceInfo>();
        private BluetoothLEAdvertisementWatcher watcher;
        
        private readonly object deviceLocker = new object();
        private BTDeviceInfo bluetoothDevice;
        private BleControl bleControl;

        static BluetoothManager() 
        {
            try
            {
                foreach (var item in JToken.Parse(File.ReadAllText("GalakuDevice\\device_items.json"))["items"])
                {
                    devices_list[item["localName"].Value<string>()] = new BTDeviceInfo(item as JObject);
                }
            } catch { }
        }

        public BluetoothManager()
        {
            watcher = new BluetoothLEAdvertisementWatcher();
            watcher.Received += OnAdvertisementReceived;
            watcher.Stopped += OnAdvertisementStopped;
            try
            {
                watcher.Start();
            }
            catch (Exception ex)
            {
                throw new Exception($"无法查找蓝牙设备: {ex.Message}");
            }
        }

        void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (bluetoothDevice == null)
            {
                var localDevice = devices_list[args.Advertisement.LocalName];
                if (localDevice != null && localDevice.IsBleDevice)
                {
                    bluetoothDevice = localDevice;
                    ConnectToDevice(args.BluetoothAddress);
                }
            }
        }

        void OnAdvertisementStopped(BluetoothLEAdvertisementWatcher a, BluetoothLEAdvertisementWatcherStoppedEventArgs b)
        {
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        watcher?.Start();
                        return;
                    }
                    catch { }
                }
            }).Start();
        }

        void ResetConnection()
        {
            lock (deviceLocker)
            {
                bleControl?.Close();
                bleControl = null;
                bluetoothDevice = null;
            }
        }

        async void ConnectToDevice(ulong address)
        {
            try
            {
                var control = await BleControl.CreateBleControl(bluetoothDevice, await BluetoothLEDevice.FromBluetoothAddressAsync(address));
                if (control?.IsConnected() == true)
                {
                    bleControl = control;
                    Console.WriteLine($"连接到蓝牙设备: {bluetoothDevice.DisplayName}");
                    PrintInputKeyUsage();
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接蓝牙失败: {ex}");
            }
            ResetConnection();
        }

        private bool CheckConnection()
        {
            if (bleControl?.IsConnected() == false)
            {
                Console.WriteLine($"蓝牙设备已断开: {bluetoothDevice.DisplayName}");
                ResetConnection();
            }
            return bleControl != null;
        }

        void PrintInputKeyUsage()
        {
            if (bluetoothDevice.DeviceTypeInt == (int)DeviceType.DeviceShakeTwo)
            {
                Console.WriteLine("按键Z、X、C分别控制拍打、震动、加热的开关，其它按键显示当前状态");
                bleControl.SendHotLevel(bluetoothDevice.HotLevel);
                bleControl.SendControl();
                return;
            }
        }

        public DeviceInfo GetDeviceInfo()
        {
            lock (deviceLocker)
            {
                if (!CheckConnection())
                    return null;
                if (bluetoothDevice.DeviceTypeInt == (int)DeviceType.DeviceShakeTwo)
                {
                    return new DeviceInfo
                    {
                        DeviceName = bluetoothDevice.DisplayName,
                        DeviceMessages = new
                        {
                            ScalarCmd = new object[]
                            {
                                new
                                {
                                    StepCount = 100,
                                    FeatureDescriptor = "马达震动数值",
                                    ActuatorType = "Vibrate",
                                },
                            }
                        }
                    };
                }
                return null;
            }
        }

        public void ExecuteCommand(string name, JToken data)
        {
            lock (deviceLocker)
            {
                if (!CheckConnection() || name == null || data == null)
                    return;
                if (bluetoothDevice.DeviceTypeInt == (int)DeviceType.DeviceShakeTwo)
                {
                    if (name == "ScalarCmd")
                    {
                        double Scalar = -1;
                        foreach (var item in data["Scalars"])
                        {
                            if (item["ActuatorType"].Value<string>() == "Vibrate"){
                                Scalar = item["Scalar"].Value<double>();
                                break;
                            }
                        }
                        if (Scalar == -1)
                        {
                            Scalar = data["Scalars"].First()["Scalar"].Value<double>();
                        }
                        // 马达震动幅度就是0~100
                        bluetoothDevice.MadaValueA = (int) Math.Round(100 * Scalar);
                        bleControl.SendControl();
                    }
                    return;
                }
            }
        }

        public void ExecuteInputKey(ConsoleKey inputKey)
        {
            lock (deviceLocker)
            {
                if (!CheckConnection())
                    return;
                if (inputKey == ConsoleKey.Z)
                {
                    bluetoothDevice.MadaEnableA = !bluetoothDevice.MadaEnableA;
                    Console.WriteLine((bluetoothDevice.MadaEnableA ? "开启" : "关闭") + "拍打");
                }
                if (inputKey == ConsoleKey.X)
                {
                    bluetoothDevice.MadaEnableB = !bluetoothDevice.MadaEnableB;
                    Console.WriteLine((bluetoothDevice.MadaEnableB ? "开启" : "关闭") + "震动");
                }
                if (inputKey == ConsoleKey.C)
                {
                    bluetoothDevice.HotLevel = bluetoothDevice.HotLevel != 0 ? 0 : 1;
                    bleControl.SendHotLevel(bluetoothDevice.HotLevel);
                    Console.WriteLine((bluetoothDevice.HotLevel == 0 ? "开启" : "关闭") + "加热");
                }
                bleControl.SendControl();
                Console.WriteLine($"当前状态: 拍打={bluetoothDevice.MadaEnableA} 震动={bluetoothDevice.MadaEnableB} 加热={bluetoothDevice.HotLevel==0}");
            }
        }

        public void StopDevice()
        {
            lock (deviceLocker)
            {
                if (!CheckConnection())
                    return;
                bluetoothDevice.MadaValueA = 0;
                bleControl.SendControl();
            }
        }
    }
}
