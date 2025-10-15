using ButtplugIo.GalakuDevice;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace ButtplugIo
{
    public class Program
    {
        // 监听的URL和端口
        private const string ServerUrl = "http://127.0.0.1:12345/";

        private static readonly List<Action<ConsoleKey>> consoleInputActions = new List<Action<ConsoleKey>>();
        private static WebSocket tempWebSocket;

        public static async Task Main(string[] args)
        {
            DeviceEngine.GetDeviceInfo();
            Console.WriteLine($"Buttplug 服务器正在监听: {ServerUrl}");

            // HttpListener 用于接受 HTTP 请求，然后升级到 WebSocket
            using (var listener = new HttpListener())
            {
                try
                {
                    listener.Prefixes.Add(ServerUrl);
                    listener.Start();

                    // 持续监听连接请求
                    while (true)
                    {
                        // 等待传入的连接
                        var context = await listener.GetContextAsync();

                        // 检查请求是否是 WebSocket 升级请求
                        if (context.Request.IsWebSocketRequest)
                        {
                            await ProcessWebSocketRequest(context);
                        }
                        else
                        {
                            // 非 WebSocket 请求，返回错误或忽略
                            context.Response.StatusCode = 400; // Bad Request
                            context.Response.Close();
                        }
                    }
                }
                catch (HttpListenerException ex)
                {
                    Console.WriteLine($"启动 HttpListener 失败: {ex}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发生错误: {ex}");
                }
            }
        }

        /// <summary>
        /// 处理单个 WebSocket 连接
        /// </summary>
        private static async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            Console.WriteLine("\n检测到 WebSocket 连接...");

            // 接受 WebSocket 连接
            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(null);
            }
            catch (Exception e)
            {
                // 接受连接失败
                context.Response.StatusCode = 500;
                context.Response.Close();
                Console.WriteLine($"接收 WebSocket 失败: {e}");
                return;
            }

            using (WebSocket webSocket = webSocketContext.WebSocket)
            {
                Console.WriteLine("WebSocket 已连接。");

                var buffer = new byte[1024 * 4];
                var receiveBuffer = new ArraySegment<byte>(buffer);

                while (webSocket.State == WebSocketState.Open)
                {
                    // 1. 接收 JSON 消息
                    WebSocketReceiveResult result;
                    try
                    {
                        tempWebSocket = webSocket;
                        result = await webSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);
                        tempWebSocket = null;
                    }
                    catch (WebSocketException)
                    {
                        // 客户端强制断开连接
                        break;
                    }

                    // 处理断开连接
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }

                    // 仅处理文本消息 (JSON)
                    if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
                    {
                        var receivedJson = Encoding.UTF8.GetString(receiveBuffer.Array, 0, result.Count);
                        var jsonTokens = ParseJsonRequest(receivedJson);
                        if (jsonTokens.Count == 0)
                        {
                            Console.WriteLine($"<<< JSON 解析失败: {receivedJson}");
                        }
                        foreach (var jsonToken in jsonTokens)
                        {
                            var processThread = new ProcessRequest();
                            var responseJson = processThread.ProcessJsonToken(jsonToken);
                            if (responseJson == null)
                            {
                                Console.WriteLine($"<<< 处理失败: {receivedJson}");
                                break;
                            }

                            if (!processThread.skipLogSendingJson)
                            {
                                Console.WriteLine($"<<< 收到消息: {receivedJson}");
                                Console.WriteLine($">>> 发送响应: {responseJson}");
                            }

                            tempWebSocket = webSocket;
                            if (!await WebSocketSendText(responseJson))
                            {
                                break; // 发送失败了，可能连接已经关闭
                            }
                            tempWebSocket = null;
                        }
                    }
                }

                tempWebSocket = null;
                Console.WriteLine("WebSocket 已关闭。");
                DeviceEngine.StopDevice();
            }
        }

        private static List<JToken> ParseJsonRequest(string receivedJson)
        {
            var tokens = new List<JToken>(1);
            using (var sr = new StringReader(receivedJson))
            using (var reader = new JsonTextReader(sr) { SupportMultipleContent = true })
            {
                while (true)
                {
                    try
                    {
                        if (!reader.Read())
                            break;
                        tokens.Add(JToken.ReadFrom(reader));
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            return tokens;
        }

        public static async Task<bool> WebSocketSendText(string text, bool log = false)
        {
            try
            {
                if (tempWebSocket == null)
                    return false;
                var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(text));
                await tempWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                if (log)
                {
                    Console.WriteLine($">>> 发送回复: {text}");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void AddConsoleInputKeyAction(Action<ConsoleKey> action)
        {
            lock (consoleInputActions)
            {
                consoleInputActions.Add(action);
                if (consoleInputActions.Count > 0)
                {
                    new Thread(() =>
                    {
                        while (true)
                        {
                            var key = Console.ReadKey(true).Key;
                            lock (consoleInputActions)
                            {
                                foreach (var item in consoleInputActions)
                                {
                                    item(key);
                                }
                            }
                        }
                    }).Start();
                }
            }
        }
    }
}
