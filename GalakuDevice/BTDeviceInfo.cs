using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButtplugIo.GalakuDevice
{
    public class BTDeviceInfo
    {
        // -----------------------------------------------------------
        // 公共字段 (转换为 C# 自动属性)
        // -----------------------------------------------------------
        public string Address { get; set; }
        public int Battery { get; set; }
        public int DeviceId { get; set; }

        // 存储设备类型的代码。注意：Java 中是 int，如果需要强类型，可以改为 DeviceType 枚举
        public int DeviceTypeInt { get; set; }

        public string DisplayName { get; set; }
        public int HotLevel { get; set; }
        public bool IsBleDevice { get; set; }
        public bool IsHotDevice { get; set; }
        public bool IsStrong { get; set; }
        public string LocalName { get; set; }
        public int MadaCount { get; set; }

        // -----------------------------------------------------------
        // 私有字段 (转换为 C# 属性，保留 Java 的 set 逻辑)
        // -----------------------------------------------------------
        private bool _connected;
        public bool Connected
        {
            get { return _connected; }
            set { _connected = value; } // 对应 Java 的 getConnected/setConnected
        }

        private int _dianjiFrequency;
        public int DianjiFrequency
        {
            get { return _dianjiFrequency; }
            set
            {
                if (_dianjiFrequency != value)
                {
                    _dianjiFrequency = value;
                    IsDataChanged = true;
                }
            }
        }

        private int _dianjiIntensity;
        public int DianjiIntensity
        {
            get { return _dianjiIntensity; }
            set
            {
                if (_dianjiIntensity != value)
                {
                    _dianjiIntensity = value;
                    IsDataChanged = true;
                }
            }
        }

        // IsDataChanged 在 Java 中是 public 字段，但通常用于内部逻辑
        public bool IsDataChanged { get; set; }


        // --- Mada Enable 属性 (带有 isDataChanged 逻辑) ---
        private bool _madaEnableA;
        public bool MadaEnableA
        {
            get { return _madaEnableA; }
            set
            {
                if (_madaEnableA != value)
                {
                    _madaEnableA = value;
                    IsDataChanged = true;
                }
            }
        }

        private bool _madaEnableB;
        public bool MadaEnableB
        {
            get { return _madaEnableB; }
            set
            {
                if (_madaEnableB != value)
                {
                    _madaEnableB = value;
                    IsDataChanged = true;
                }
            }
        }

        private bool _madaEnableC;
        public bool MadaEnableC
        {
            get { return _madaEnableC; }
            set
            {
                if (_madaEnableC != value)
                {
                    _madaEnableC = value;
                    IsDataChanged = true;
                }
            }
        }

        // --- Mada Value 属性 (带有 isDataChanged 逻辑) ---
        private int _madaValueA;
        public int MadaValueA
        {
            get { return _madaValueA; }
            set
            {
                if (_madaValueA != value)
                {
                    _madaValueA = value;
                    IsDataChanged = true;
                }
            }
        }

        private int _madaValueB;
        public int MadaValueB
        {
            get { return _madaValueB; }
            set
            {
                if (_madaValueB != value)
                {
                    _madaValueB = value;
                    IsDataChanged = true;
                }
            }
        }

        private int _madaValueC;
        public int MadaValueC
        {
            get { return _madaValueC; }
            set
            {
                if (_madaValueC != value)
                {
                    _madaValueC = value;
                    IsDataChanged = true;
                }
            }
        }

        // -----------------------------------------------------------
        // 构造函数 (Constructor)
        // -----------------------------------------------------------

        public BTDeviceInfo()
        {
            // 默认构造函数
        }

        /// <summary>
        /// 使用 JObject (相当于 Java JSONObject) 初始化 BTDeviceInfo。
        /// </summary>
        public BTDeviceInfo(JObject jObject)
        {
            // 尝试解析 JSON，捕获异常以模拟 Java 的 try-catch(unused) 结构
            try
            {
                // Java: if (!jSONObject.isNull("localName"))
                // C#: 检查 JObject 是否包含键，并且值不为 null 或 JTokenType.Null
                if (jObject.TryGetValue("localName", out JToken tokenLocalName) && tokenLocalName.Type != JTokenType.Null)
                {
                    this.LocalName = tokenLocalName.ToObject<string>();
                }

                if (jObject.TryGetValue("displayName", out JToken tokenDisplayName) && tokenDisplayName.Type != JTokenType.Null)
                {
                    this.DisplayName = tokenDisplayName.ToObject<string>();
                }

                // Java: if (jSONObject.has("deviceType"))
                // C#: 检查是否存在键
                if (jObject.TryGetValue("deviceType", out JToken tokenDeviceType))
                {
                    this.DeviceTypeInt = tokenDeviceType.ToObject<int>();
                }

                if (jObject.TryGetValue("isHotDevice", out JToken tokenIsHotDevice))
                {
                    this.IsHotDevice = tokenIsHotDevice.ToObject<bool>();
                }

                if (jObject.TryGetValue("isBleDevice", out JToken tokenIsBleDevice))
                {
                    this.IsBleDevice = tokenIsBleDevice.ToObject<bool>();
                }

                if (jObject.TryGetValue("madaCount", out JToken tokenMadaCount))
                {
                    this.MadaCount = tokenMadaCount.ToObject<int>();
                }

                if (jObject.TryGetValue("isStrong", out JToken tokenIsStrong))
                {
                    this.IsStrong = tokenIsStrong.ToObject<bool>();
                }
            }
            catch (Exception)
            {
                // 模仿 Java 的空 catch 块 (Exception unused)
            }
        }

        // -----------------------------------------------------------
        // 公共方法
        // -----------------------------------------------------------

        public BTDeviceInfo CopyOne()
        {
            // 使用 C# 简洁的初始化语法
            BTDeviceInfo bTDeviceInfo = new BTDeviceInfo
            {
                LocalName = this.LocalName,
                DisplayName = this.DisplayName,
                DeviceTypeInt = this.DeviceTypeInt,
                IsHotDevice = this.IsHotDevice,
                IsBleDevice = this.IsBleDevice,
                MadaCount = this.MadaCount,
                IsStrong = this.IsStrong
            };

            bTDeviceInfo.EnableAll();
            return bTDeviceInfo;
        }

        public void EnableAll()
        {
            // 直接设置支持字段或属性
            this._madaEnableA = true;
            this._madaEnableB = true;
            this._madaEnableC = true;
            // 注意：由于这里直接设置了支持字段，IsDataChanged 不会被设置为 true，
            // 保持与 Java 原逻辑一致。
        }

        public bool CanShowABFloating()
        {
            // 使用 DeviceType 枚举进行比较，并获取其底层 int 值
            int i;
            return this.DeviceTypeInt == (int)DeviceType.DeviceShakeAndDianji
                   || this.DeviceTypeInt == (int)DeviceType.DeviceShakeAndDianji2
                   || (i = this.MadaCount) == 2
                   || i == 3;
        }

        public bool IsPumpDevice()
        {
            return this.DeviceTypeInt == (int)DeviceType.DeviceShakeAndPump;
        }

        /// <summary>
        /// 模拟 Java 的 setIntensity 方法。
        /// </summary>
        public void SetIntensity(float f)
        {
            // Java: int max = Math.max(0, Math.min(100, (int) (100.0f * f)));
            int max = Math.Max(0, Math.Min(100, (int)(100.0f * f)));

            // 调用属性的 set 访问器，这会触发 IsDataChanged = true 逻辑
            this.MadaValueA = max;
            this.MadaValueB = max;

            this.DianjiFrequency = 4;

            // Java: setDianjiIntensity((int) Math.ceil(f * 6.0f));
            this.DianjiIntensity = (int)Math.Ceiling(f * 6.0f);

            // Java 原代码在 setIntensity 方法最后又设置了一次 isDataChanged = true;
            this.IsDataChanged = true;
        }
    }

    public enum DeviceType
    {
        DeviceShakeOne = 0,
        DeviceShakeTwo = 1,
        DeviceShakeAndPump = 2,
        DeviceShakeAndDianji = 3,
        DeviceShakeTwoAndDianji = 4,
        DeviceShakeAndDianji2 = 5,
        DeviceXiAndChouCha = 6,
        DeviceChouCha = 7,
        DeviceShakeThree = 8
    }
}
