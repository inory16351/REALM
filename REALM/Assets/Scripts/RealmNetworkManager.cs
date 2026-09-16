using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Realm
{
    public class RealmNetworkManager : MonoBehaviour
    {
        public static RealmNetworkManager Instance { get; private set; }

        private const string ApiBase = "https://crown-discard-online.caddman771144.workers.dev/api";
        private const string WsBase = "wss://crown-discard-online.caddman771144.workers.dev/api";

        public event Action<GameState> OnStateUpdated;
        
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private string _sessionCookie;

        [Serializable]
        private class RoomCreateRequest
        {
            public string name;
            public int playerTarget;
            public int discussionSeconds;
        }

        [Serializable]
        private class RoomJoinRequest
        {
            public string name;
            public string code;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _incomingMessages = new System.Collections.Concurrent.ConcurrentQueue<string>();

        private void Update()
        {
            while (_incomingMessages.TryDequeue(out var json))
            {
                ProcessMessage(json);
            }
        }

        public async Task<string> CreateRoom(string playerName, int targetPlayers, int discussionTime)
        {
            var req = new RoomCreateRequest { name = playerName, playerTarget = targetPlayers, discussionSeconds = discussionTime };
            var json = JsonUtility.ToJson(req);
            using var www = new UnityWebRequest($"{ApiBase}/rooms", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            var op = www.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"CreateRoom Error: {www.error}\nBody: {www.downloadHandler?.text}");
                return null;
            }

            ExtractSessionCookie(www);

            string responseText = www.downloadHandler.text;
            Debug.Log($"CreateRoom Response: {responseText}");

            string code = null;
            var match = System.Text.RegularExpressions.Regex.Match(responseText, @"""roomCode""\s*:\s*""([^""]+)""");
            if (match.Success)
            {
                code = match.Groups[1].Value;
            }
            else
            {
                var parts = www.url.Split('/');
                var last = parts[parts.Length - 1];
                if (last != "rooms" && !string.IsNullOrEmpty(last)) code = last;
            }

            Debug.Log($"[CreateRoom] Successfully created room with code: {code}");
            return code;
        }

        public async Task<bool> JoinRoom(string playerName, string roomCode)
        {
            var req = new RoomJoinRequest { name = playerName, code = roomCode };
            var json = JsonUtility.ToJson(req);
            using var www = new UnityWebRequest($"{ApiBase}/rooms/{roomCode}/join", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            var op = www.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"JoinRoom Error: {www.error}\nBody: {www.downloadHandler?.text}");
                return false;
            }

            ExtractSessionCookie(www);
            Debug.Log($"[JoinRoom] Successfully joined room {roomCode}: {www.downloadHandler.text}");
            return true;
        }

        private void ExtractSessionCookie(UnityWebRequest www)
        {
            var cookieHeader = www.GetResponseHeader("Set-Cookie");
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                var match = System.Text.RegularExpressions.Regex.Match(cookieHeader, @"crown_session=([^;]+)");
                if (match.Success)
                {
                    _sessionCookie = match.Groups[1].Value;
                    Debug.Log($"Session cookie acquired: {_sessionCookie}");
                }
            }
        }

        public async void ConnectWebSocket(string roomCode)
        {
            Disconnect();
            _cts = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();
            
            if (!string.IsNullOrEmpty(_sessionCookie))
            {
                _webSocket.Options.SetRequestHeader("Cookie", $"crown_session={_sessionCookie}");
            }

            var wsUri = new Uri($"{WsBase}/rooms/{roomCode}/ws");
            Debug.Log($"[ConnectWebSocket] Connecting to {wsUri} with cookie: {_sessionCookie}");
            try
            {
                await _webSocket.ConnectAsync(wsUri, _cts.Token);
                Debug.Log("WebSocket connected successfully!");
                _ = ReceiveLoop();
            }
            catch (Exception e)
            {
                Debug.LogError($"WebSocket Connection Error: {e.Message}");
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[1024 * 16];
            while (_webSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        Debug.Log("WebSocket closed");
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _incomingMessages.Enqueue(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"WebSocket Receive Error: {e.Message}");
                    break;
                }
            }
        }

        private void ProcessMessage(string json)
        {
            // Unity's JsonUtility handles parsing
            // The JSON from backend is { "type": "STATE", "state": { ... } }
            try
            {
                var msg = JsonUtility.FromJson<StateMessage>(json);
                if (msg != null && msg.type == "STATE" && msg.state != null)
                {
                    OnStateUpdated?.Invoke(msg.state);
                }
                else
                {
                    Debug.Log($"[WS Non-State Message]: {json}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"JSON Parse Error: {e.Message}\nJSON: {json}");
            }
        }

        public async void SendMessagePayload(string type, object payload = null)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                Debug.LogWarning($"[WS Send Failed] Socket not open. State: {_webSocket?.State}");
                return;
            }

            // Manual JSON construction to wrap payload
            string payloadJson = payload != null ? JsonUtility.ToJson(payload) : "{}";
            string json = $"{{\"type\":\"{type}\", \"payload\":{payloadJson}}}";
            Debug.Log($"[WS Send] {json}");

            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (_webSocket != null)
            {
                if (_webSocket.State == WebSocketState.Open)
                    _webSocket.Abort();
                _webSocket.Dispose();
                _webSocket = null;
            }
        }
    }
}
