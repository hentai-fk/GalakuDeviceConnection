using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButtplugIo
{
    public interface DeviceManager
    {
        DeviceInfo GetDeviceInfo();
        void ExecuteCommand(string name, JToken data);
        void ExecuteInputKey(ConsoleKey inputKey);
        void StopDevice();
    }
}
