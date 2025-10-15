using Newtonsoft.Json.Linq;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButtplugIo
{
    public interface DeviceManager
    {
        DeviceInfo GetDeviceInfo();
        void ExecuteCommand(string name, JObject data);
        void ExecuteInputKey(ConsoleKey inputKey);
        void ExecuteGlobalKey(KeyEventArgs inputKey, bool isKeyDown);
        void StopDevice();
    }
}
