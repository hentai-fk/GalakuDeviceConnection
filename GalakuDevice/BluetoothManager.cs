using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        private LinkedList<Action> actionSendControl = new LinkedList<Action>();

        private bool toggleScale2Linear;
        private bool globalEnterBurstMode;
        private bool globalInputKeyQ;
        private bool globalInputKeyW;
        private bool globalInputKeyE;

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
            // 发送命令的线程
            Task.Run(() =>
            {
                while (true)
                {

                    while (true)
                    {
                        Action action = null;
                        lock (actionSendControl)
                        {
                            action = actionSendControl.FirstOrDefault();
                            if (action == null)
                                break;
                            actionSendControl.RemoveFirst();
                        }
                        action();
                    }
                    lock (actionSendControl)
                    {
                        Monitor.Wait(actionSendControl);
                    }
                }
            });
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

        void SendControl(Action action, bool clear = false, bool skip = false, int sleep = 0, int index = -1)
        {
            Action todo = () =>
            {
                lock (deviceLocker)
                {
                    if (!CheckConnection())
                        return;
                    action();
                    if (!skip)
                        bleControl.SendControl();
                }
                if (sleep > 0)
                    Thread.Sleep(sleep);
            };
            lock (actionSendControl)
            {
                if (clear)
                    actionSendControl.Clear();
                if (index >= 0 && actionSendControl.Count > index)
                {
                    actionSendControl.AddBefore(actionSendControl.Find(actionSendControl.ElementAt(index)), todo);
                }
                else 
                {
                    actionSendControl.AddLast(todo);
                }
                Monitor.PulseAll(actionSendControl);
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

        bool CheckConnection()
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
                Console.WriteLine("按键Z、X、C、V分别控制拍打、震动、插入越深越强烈模式、加热的开关，按键A、S控制强度加减，同时按下W、E、R任意两个按键进入爆发模式然后全部松开这三个按键之后进入静止状态，其它按键显示当前状态");
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
                                    FeatureDescriptor = "震动强度数值，0~100",
                                    ActuatorType = "Vibrate",
                                },
                            },
                            LinearCmd = new object[]
                            {
                                new
                                {
                                    StepCount = 100,
                                    FeatureDescriptor = "线性伸缩高度，0~100",
                                    ActuatorType = "Position",
                                },
                            }
                        }
                    };
                }
                return null;
            }
        }

        public void ExecuteCommand(string name, JObject data)
        {
            lock (deviceLocker)
            {
                if (!CheckConnection() || name == null || data == null)
                    return;
                if (bluetoothDevice.DeviceTypeInt == (int)DeviceType.DeviceShakeTwo)
                {
                    if (name == "ScalarCmd" || name == "LinearCmd")
                    {
                        if (!toggleScale2Linear)
                        {
                            if (name != "ScalarCmd")
                                return;
                            double Scalar = -1;
                            foreach (var item in data["Scalars"])
                            {
                                if (item["ActuatorType"].Value<string>() == "Vibrate")
                                {
                                    Scalar = item["Scalar"].Value<double>();
                                    break;
                                }
                            }
                            if (Scalar == -1)
                            {
                                Scalar = data["Scalars"].First()["Scalar"].Value<double>();
                            }
                            SendControl(() =>
                            {
                                // 马达震动幅度就是0~100
                                bluetoothDevice.MadaValueA = (int)Math.Round(100 * Scalar);
                            }, true);
                        }
                        else
                        {
                            if (name != "LinearCmd")
                                return;
                            var Vector = data["Vectors"].First();
                            var Position = Vector["Position"].Value<double>();
                            var Duration = Vector["Duration"].Value<int>();
                            SendControl(() =>
                            {
                                foreach (var item in MathCalScaler.CalScalerWithTime(bluetoothDevice.MadaValueA / 100.0, Position, Duration, 30))
                                {
                                    SendControl(() =>
                                    {
                                        bluetoothDevice.MadaValueA = (int) Math.Round(item * 100);
                                    }, false, false, 30);
                                }
                            }, true, true);
                        }
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
                if (globalEnterBurstMode)
                    return;
                if (inputKey == ConsoleKey.Z)
                {
                    SendControl(() =>
                    {
                        bluetoothDevice.MadaEnableA = !bluetoothDevice.MadaEnableA;
                        Console.WriteLine((bluetoothDevice.MadaEnableA ? "开启" : "关闭") + "拍打");
                    }, false, false, 0, 0);
                }
                if (inputKey == ConsoleKey.X)
                {
                    SendControl(() =>
                    {
                        bluetoothDevice.MadaEnableB = !bluetoothDevice.MadaEnableB;
                        Console.WriteLine((bluetoothDevice.MadaEnableB ? "开启" : "关闭") + "震动");
                    }, false, false, 0, 0);
                }
                if (inputKey == ConsoleKey.C)
                {
                    toggleScale2Linear = !toggleScale2Linear;
                    Console.WriteLine(toggleScale2Linear ? "插入越深越强烈模式" : "震动数值模式");
                }
                if (inputKey == ConsoleKey.V)
                {
                    bluetoothDevice.HotLevel = bluetoothDevice.HotLevel != 0 ? 0 : 1;
                    bleControl.SendHotLevel(bluetoothDevice.HotLevel);
                    Console.WriteLine((bluetoothDevice.HotLevel == 0 ? "开启" : "关闭") + "加热");
                }
                if (inputKey == ConsoleKey.A || inputKey == ConsoleKey.S)
                {
                    SendControl(() =>
                    {
                        bluetoothDevice.MadaValueA = Math.Max(0, Math.Min(100, bluetoothDevice.MadaValueA + (inputKey == ConsoleKey.A ? -5 : 5)));
                        Console.WriteLine((inputKey == ConsoleKey.A ? "降低" : "提高") + "强度到" + bluetoothDevice.MadaValueA);
                    }, false, false, 0, 0);
                }
                SendControl(() => Console.WriteLine($"当前状态: 拍打={bluetoothDevice.MadaEnableA} 震动={bluetoothDevice.MadaEnableB} 强度={bluetoothDevice.MadaValueA} 插入越深越强烈模式={toggleScale2Linear} 加热={bluetoothDevice.HotLevel==0} 电源={bleControl.GetBatteryText()}"), false, false, 0, 1);
            }
        }

        public void ExecuteGlobalKey(KeyEventArgs inputKey, bool isKeyDown)
        {
            lock (deviceLocker)
            {
                if (!CheckConnection())
                    return;
                if (inputKey.KeyCode == Keys.Q)
                    globalInputKeyQ = isKeyDown;
                if (inputKey.KeyCode == Keys.W)
                    globalInputKeyW = isKeyDown;
                if (inputKey.KeyCode == Keys.E)
                    globalInputKeyE = isKeyDown;
                var burstValue = (globalInputKeyQ ? 1 : 0) + (globalInputKeyW ? 1 : 0) + (globalInputKeyE ? 1 : 0);
                if (burstValue >= 2 && !globalEnterBurstMode)
                {
                    globalEnterBurstMode = true;
                    Console.WriteLine("进入爆发模式");
                }
                if (globalEnterBurstMode)
                {
                    if (burstValue == 0)
                    {
                        globalEnterBurstMode = false;
                        Console.WriteLine("开始冷静状态");
                        SendControl(() =>
                        {
                            bluetoothDevice.MadaValueA = 0;
                            bluetoothDevice.MadaEnableA = false;
                            bluetoothDevice.MadaEnableB = false;
                        }, true);
                    }
                    else
                    {
                        SendControl(() =>
                        {
                            bluetoothDevice.MadaValueA = 100;
                            bluetoothDevice.MadaEnableA = true;
                            bluetoothDevice.MadaEnableB = true;
                        }, true);
                    }
                }
            }
        }

        public void StopDevice()
        {
            lock (deviceLocker)
            {
                if (!CheckConnection())
                    return;
                SendControl(() =>
                {
                    bluetoothDevice.MadaValueA = 0;
                    bluetoothDevice.MadaEnableA = false;
                    bluetoothDevice.MadaEnableB = false;
                }, true);
            }
        }
    }
}
