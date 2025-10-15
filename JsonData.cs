using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButtplugIo
{
    public class JsonData
    {
        private readonly JToken token;

        public JsonData(JToken token) 
        { 
            this.token = token;
        }

        public JToken GetNode(string key)
        {
            // [{"RequestServerInfo":{"ClientName":"MultiFunPlayer","MessageVersion":3,"Id":1}}]
            // 检查根元素是否是数组
            if (token?.Type != JTokenType.Array)
            {
                return null;
            }

            var jsonArray = (JArray) token;

            // 2. 检查数组是否为空
            if (jsonArray.Count == 0)
            {
                return null;
            }

            // 3. 访问数组
            foreach (var item in jsonArray)
            {
                if (item.Type == JTokenType.Object)
                {
                    if (key == null)
                    {
                        return ((JObject)item).Properties().FirstOrDefault();
                    }
                    var obj = ((JObject) item)[key];
                    if (obj != null)
                    {
                        return obj;
                    }
                }
            }

            return null;
        }

        public JProperty GetNode()
        {
            return GetNode(null) as JProperty;
        }
    }
}
