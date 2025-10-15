using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButtplugIo
{
    public class ProcessRequest
    {
        public bool skipLogSendingJson;

        public string ProcessJsonToken(JToken json)
        {
            try
            {
                return ProcessJson(new JsonData(json)).ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: 处理命令失败: {ex}");
            }
            return null;
        }

        private JToken ProcessJson(JsonData data)
        {
            var map = new Dictionary<string, object>();
            GenerateMapResult(data, map);
            return JsonConvert.SerializeObject(new object[] { map });
        }

        private void GenerateMapResult(JsonData data, Dictionary<string, object> map)
        {
            if (data.GetNode("RequestServerInfo") != null)
            {
                var item = data.GetNode("RequestServerInfo");
                Console.WriteLine($"*** 连接到客户端: {item["ClientName"]} ***");
                map["ServerInfo"] = new
                {
                    Id = item["Id"],
                    ServerName = "HentaiSimulator",
                    MessageVersion = 3,
                    MaxPingTime = "1000",
                };
                return;
            }

            if (data.GetNode("RequestDeviceList") != null)
            {
                var item = data.GetNode("RequestDeviceList");
                var deviceInfo = DeviceEngine.GetDeviceInfo();

                var Devices = new List<object>();

                if (deviceInfo != null) {
                    Devices.Add(new
                    {
                        DeviceIndex = 0,
                        deviceInfo.DeviceName,
                        deviceInfo.DeviceMessages,
                    });
                }

                map["DeviceList"] = new
                {
                    Id = item["Id"],
                    Devices,
                };

                DeviceEngine.SetScanningState(false);
                return;
            }

            if (data.GetNode("StartScanning") != null)
            {
                DeviceEngine.SetScanningState(true);
            }

            if (data.GetNode("StopScanning") != null)
            {
                DeviceEngine.SetScanningState(false);
            }

            if (data.GetNode("ScalarCmd") != null || data.GetNode("LinearCmd") != null)
            {
                var item = data.GetNode();
                DeviceEngine.ResolveCommandData(item.Name, item.Value as JObject);
            }

            if (data.GetNode("StopDeviceCmd") != null || data.GetNode("StopAllDevices") != null)
            {
                DeviceEngine.StopDevice();
            }

            if (data.GetNode("Ping") != null)
            {
                skipLogSendingJson = true;
            }

            map["Ok"] = new
            {
                Id = data.GetNode()?.Value?["Id"],
            };
        }
    }
}
