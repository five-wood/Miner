using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Miner.Utils
{
    public class SocketManager
    {
        private static SocketManager _instance;
        private TcpClient _client;
        private NetworkStream _stream;
        private string _host = "127.0.0.1";
        private int _port = 9000;
        private bool _isConnected = false;

        public static SocketManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SocketManager();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 初始化Socket连接
        /// </summary>
        /// <param name="host">服务器地址</param>
        /// <param name="port">端口号</param>
        public void Initialize(string host = "127.0.0.1", int port = 9000)
        {
            _host = host;
            _port = port;
            Connect();
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public bool Connect()
        {
            try
            {
                if (_isConnected)
                {
                    Debug.Log("[SocketManager] Already connected.");
                    return true;
                }

                _client = new TcpClient();
                _client.Connect(_host, _port);
                _stream = _client.GetStream();
                _isConnected = true;
                Debug.Log($"[SocketManager] Connected to {_host}:{_port}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SocketManager] Failed to connect: {e.Message}");
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">要发送的消息</param>
        public void Send(string message)
        {
            if (!_isConnected)
            {
                Debug.LogWarning("[SocketManager] Not connected. Attempting to reconnect...");
                if (!Connect())
                {
                    Debug.LogError("[SocketManager] Failed to send message: not connected.");
                    return;
                }
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                _stream.Write(data, 0, data.Length);
                Debug.Log($"[SocketManager] Sent: {message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketManager] Failed to send message: {e.Message}");
                _isConnected = false;
            }
        }

        /// <summary>
        /// 发送关卡开始消息
        /// </summary>
        /// <param name="level">关卡编号</param>
        public void SendLevelStart(int level)
        {
            string message = $"{{\"type\":\"level_start\",\"level\":{level}}}\n";
            Send(message);
        }

        /// <summary>
        /// 发送关卡结束消息
        /// </summary>
        /// <param name="level">关卡编号</param>
        /// <param name="isWin">是否胜利</param>
        /// <param name="score">得分</param>
        public void SendLevelEnd(int level, bool isWin, int score)
        {
            string message = $"{{\"type\":\"level_end\",\"level\":{level},\"isWin\":{isWin.ToString().ToLower()},\"score\":{score}}}\n";
            Send(message);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (_stream != null)
                {
                    _stream.Close();
                    _stream = null;
                }
                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }
                _isConnected = false;
                Debug.Log("[SocketManager] Disconnected.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketManager] Error while disconnecting: {e.Message}");
            }
        }

        /// <summary>
        /// 检查是否已连接
        /// </summary>
        public bool IsConnected => _isConnected;
    }
}

