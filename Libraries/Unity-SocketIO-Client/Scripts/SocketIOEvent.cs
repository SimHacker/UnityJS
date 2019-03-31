#if USE_SOCKETIO && UNITY_EDITOR

namespace Dpoch.SocketIO {
    using System;
    using UnityEngine;
    using Newtonsoft.Json.Linq;

    public class SocketIOEvent {
        /// <summary>
        /// The name of the event
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The data that was sent with the event. Elements ([0], [1], etc.) correspond to the emit parameters.
        /// </summary>
        public JArray Data { get; private set; }

        /// <summary>
        /// Whether this event is acknowledgable
        /// </summary>
        public bool IsAcknowledgable { get { return ack != null && !ackSent; } }

        Action<object[]> ack { get; set; }
        bool ackSent { get; set; }

        public SocketIOEvent(string name, JArray data, Action<object[]> ack) {
            Name = name;
            Data = data;
            this.ack = ack;
        }

        /// <summary>
        /// Acknowledge this event
        /// </summary>
        /// <param name="data">Data to be sent with the acknowledgement</param>
        public void Acknowledge(params object[] data) {
            if(ack == null) {
                Debug.LogError("The server doesn't expect an acknowledgement for event " + Name);
                return;
            }
            if (ackSent) {
                Debug.LogError("Event " + Name + " has already been acknowledged");
                return;
            }
            ackSent = true;
            ack(data);
        }
    }
}

#endif
