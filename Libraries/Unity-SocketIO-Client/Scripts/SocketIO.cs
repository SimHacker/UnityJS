#if USE_SOCKETIO && UNITY_EDITOR

namespace Dpoch.SocketIO {
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using UnityEngine;

    public class SocketIO {
        SocketIOEventEmitter emitter;
        SocketIOConnection socketConnection;
        int nextEventID;
        Dictionary<string, List<Action<SocketIOEvent>>> eventHandlers = new Dictionary<string, List<Action<SocketIOEvent>>>();
        Dictionary<int, AckHandler> ackHandlers = new Dictionary<int, AckHandler>();
        Queue<Action> synchronizationQueue = new Queue<Action>();
        object synchronizationQueueLock = new object();
        int ackExpireTimeMS;

        /// <summary>
        /// Whether the Socket is connected
        /// </summary>
        public bool IsAlive {
            get { return socketConnection.IsAlive; }
        }

        /// <summary>
        /// Fires when the socket opened and is ready to send/receive events
        /// </summary>
        public event Action OnOpen;

        /// <summary>
        /// Fires when the connection attempt failed
        /// </summary>
        public event Action OnConnectFailed;

        /// <summary>
        /// Fires when the socket closes (i.e. it was open before) for any reason including disconnects or Close() being called
        /// </summary>
        public event Action OnClose;

        /// <summary>
        /// Fires when an error occured in the socket
        /// </summary>
        public event Action<SocketIOException> OnError;

        /// <summary>
        /// Creates a new SocketIO Object
        /// </summary>
        /// <param name="url">The url of the server to connect to. Should be a fully qualified SocketIO WebSocket url. e.g. "ws://127.0.0.1:80/socket.io/?EIO=4&transport=websocket"</param>
        /// <param name="connectTimeoutMS">Time until the socket stops connecting if the connection couldn't be established yet or it didn't receive a valid SocketIO handshake. (in milliseconds)</param>
        /// <param name="ackExpireTimeMS">Time until ack callbacks get nulled if they didn't get called. (in milliseconds)</param>
        public SocketIO(string url, int connectTimeoutMS = 30000, int ackExpireTimeMS = 300000) {
            this.ackExpireTimeMS = ackExpireTimeMS;
            socketConnection = new SocketIOConnection(new Uri(url), connectTimeoutMS);
            socketConnection.OnOpen += HandleOpen;
            socketConnection.OnConnectFailed += HandleConnectFailed;
            socketConnection.OnClose += HandleClose;
            socketConnection.OnError += HandleError;
            socketConnection.OnEvent += HandleEvent;
            socketConnection.OnAck += HandleAck;
        }

        void Reset() {
            nextEventID = 0;
            ackHandlers.Clear();
            synchronizationQueue.Clear();
        }

        /// <summary>
        /// Tries to connect to the server.
        /// </summary>
        public void Connect() {
            if (socketConnection.IsAlive) {
                Debug.LogError("Socket is still alive - can't call Connect()");
                return;
            }
            socketConnection.Connect();
            emitter = SocketIOEventEmitter.Create(this);
        }

        /// <summary>
        /// Closes the socket.
        /// </summary>
        public void Close() {
            socketConnection.Close();
        }

        /// <summary>
        /// Listen to a SocketIO event from the server.
        /// </summary>
        /// <param name="ev">The name of the event</param>
        /// <param name="handler">The event handler delegate</param>
        public void On(string ev, Action<SocketIOEvent> handler) {
            List<Action<SocketIOEvent>> handlers;
            if (!eventHandlers.TryGetValue(ev, out handlers)) {
                handlers = new List<Action<SocketIOEvent>>();
                eventHandlers.Add(ev, handlers);
            }
            handlers.Add(handler);
        }

        /// <summary>
        /// Removes an event handler for the specified event.
        /// </summary>
        /// <param name="ev">The name of the event</param>
        /// <param name="handler">The event handler delegate to be removed</param>
        public void Off(string ev, Action<SocketIOEvent> handler) {
            List<Action<SocketIOEvent>> handlers;
            if (eventHandlers.TryGetValue(ev, out handlers)) {
                handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Emits an event on the socket which gets sent to the server
        /// </summary>
        /// <param name="ev">The name of the event</param>
        /// <param name="data">The data to be serialized/sent</param>
        public void Emit(string ev, params object[] data) {
            socketConnection.Send(Packet.Event(ev, data));
        }

        /// <summary>
        /// Emits an event on the socket which gets sent to the server and listens to an acknowledgement from the server
        /// </summary>
        /// <param name="ev">The name of the event</param>
        /// <param name="ackHandler">The delegate to be executed upon acknowledgement</param>
        /// <param name="data">The data to be serialized/sent</param>
        public void Emit(string ev, Action<JArray> ackHandler, params object[] data) {
            socketConnection.Send(Packet.Event(ev, data, nextEventID));
            ackHandlers[nextEventID] = new AckHandler(ackHandler);
            nextEventID++;
        }  

        void HandleOpen() {
            lock (synchronizationQueueLock) {
                synchronizationQueue.Enqueue(() => { if (OnOpen != null) OnOpen(); });
            }
        }

        void HandleConnectFailed() {
            var cachedEmitter = emitter;
            lock (synchronizationQueueLock) {
                synchronizationQueue.Enqueue(() => {
                    if(cachedEmitter != null) cachedEmitter.shouldRun = false;
                    if (OnConnectFailed != null) OnConnectFailed();
                    Reset();
                });
            }
        }

        void HandleClose() {
            var cachedEmitter = emitter;
            lock (synchronizationQueueLock) {
                synchronizationQueue.Enqueue(() => {
                    if (cachedEmitter != null) cachedEmitter.shouldRun = false;
                    if (OnClose != null) OnClose();
                    Reset();
                });
            }
        }

        void HandleError(SocketIOException error) {
            lock (synchronizationQueueLock) {
                synchronizationQueue.Enqueue(() => { if (OnError != null) OnError(error); });
            }
        }

        void HandleEvent(Packet packet) {
            var packetData = packet.Data;
            Action<object[]> ack = null;
            if (packet.ID >= 0) ack = (data) => {
                socketConnection.Send(Packet.Ack(packet.ID, data));
            };

            var ev = new SocketIOEvent(
                packetData[0].ToString(),
                new JArray(packetData.Skip(1).ToArray()),
                ack
            );

            lock (synchronizationQueueLock) {
                synchronizationQueue.Enqueue(() => {
                    List<Action<SocketIOEvent>> handlers;
                    if (eventHandlers.TryGetValue(ev.Name, out handlers)) {
                        foreach(var handler in handlers.ToArray()) { //copy handler list so handlers can call Off();
                            if (handler != null) handler(ev);
                        }
                    }
                });
            }
        }

        void HandleAck(Packet packet) {
            var data = (JArray)packet.Data;
            lock (synchronizationQueueLock) {
                synchronizationQueue.Enqueue(() => {
                    AckHandler handler;
                    if (ackHandlers.TryGetValue(packet.ID, out handler)) {
                        handler.Handler(data);
                        ackHandlers.Remove(packet.ID);
                    }
                });
            }
        }

        class AckHandler {
            public Action<JArray> Handler { get; private set; }
            public DateTime Timestamp { get; private set; }

            public AckHandler(Action<JArray> handler) {
                Handler = handler;
                Timestamp = DateTime.Now;
            }
        }

        [AddComponentMenu("")]
        class SocketIOEventEmitter : MonoBehaviour {
            SocketIO socket;
            [HideInInspector]
            public bool shouldRun = true;

            public static SocketIOEventEmitter Create(SocketIO socket) {
                var go = new GameObject("SocketIO Event Emitter");
                DontDestroyOnLoad(go);
                var emitter = go.AddComponent<SocketIOEventEmitter>();
                emitter.socket = socket;
                return emitter;
            }

            void Update() {
                lock (socket.synchronizationQueueLock) {
                    while (socket.synchronizationQueue.Count > 0) {
                        socket.synchronizationQueue.Dequeue()();
                    }
                }

                var now = DateTime.Now;
                foreach(var kv in socket.ackHandlers) {
                    if(now.Subtract(kv.Value.Timestamp).TotalMilliseconds >= socket.ackExpireTimeMS) {
                        socket.ackHandlers.Remove(kv.Key);
                    }
                }

                if (!shouldRun) Destroy(gameObject);
            }

            void OnDestroy() {
                if (shouldRun) socket.Close();
            }

            void OnApplicationQuit() {
                socket.Close();
            }
        }
    }
}

#endif
