#if USE_SOCKETIO && UNITY_EDITOR

namespace Dpoch.SocketIO {
    using System;

    public class SocketIOException : Exception {
        public SocketIOException() : base() { }
        public SocketIOException(string message) : base(message) { }
        public SocketIOException(string message, Exception innerException) : base(message, innerException) { } 
    }
}

#endif
