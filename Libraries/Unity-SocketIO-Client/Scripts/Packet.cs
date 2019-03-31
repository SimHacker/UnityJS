#if USE_SOCKETIO && UNITY_EDITOR

namespace Dpoch.SocketIO {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using UnityEngine;

    public class Packet {
        public enum EngineIOPacketType{
            UNDEFINED = -1,
            OPEN = 0,
            CLOSE = 1,
            PING = 2,
            PONG = 3,
            MESSAGE = 4,
            UPGRADE = 5,
            NOOP = 6
        }

        public enum SocketIOPacketType {
            UNDEFINED = -1,
            CONNECT = 0,
            DISCONNECT = 1,
            EVENT = 2,
            ACK = 3,
            ERROR = 4,
            BINARY_EVENT = 5,
            BINARY_ACK = 6
        }

        struct BinaryPlaceholder {
            public bool _placeholder;
            public int num;
        }

        int numAttachments;

        public EngineIOPacketType EnginePacketType { get; private set; }
        public SocketIOPacketType SocketPacketType { get; private set; }
        public int ID { get; private set; }
        public string Nsp { get; private set; }
        public List<byte[]> Attachments { get; private set; }
        public bool IsComplete { get{ return Attachments.Count >= numAttachments; } }
        JToken _data = new JArray();
        public JToken Data {
            get {
                if (!IsComplete) return null;
                JToken dataCopy = _data.DeepClone();
                if (numAttachments == 0) return dataCopy;

                ReplaceData(dataCopy, (token) => {
                    if (token.Type == JTokenType.Object) {
                        var obj = (JObject)token;
                        var placeholder = obj["_placeholder"];
                        if (placeholder != null && placeholder.Type == JTokenType.Boolean && (bool)placeholder) {
                            var num = obj["num"];
                            if (num != null && num.Type == JTokenType.Integer && (int)num < Attachments.Count) {
                                return Attachments[(int)num].Skip(1).ToArray();
                            }
                        }
                    }
                    return null;
                });
                return dataCopy;
            }
        }

        //private constructor - use factory methods instead
        Packet() {
            EnginePacketType = EngineIOPacketType.UNDEFINED;
            SocketPacketType = SocketIOPacketType.UNDEFINED;
            ID = -1;
            Nsp = "/";
            Attachments = new List<byte[]>();
        }


        public static Packet Ping() {
            return new Packet() {
                EnginePacketType = EngineIOPacketType.PING
            };
        }

        public static Packet Pong() {
            return new Packet() {
                EnginePacketType = EngineIOPacketType.PONG
            };
        }

        public static Packet Event(string ev, object[] data, int id = -1) {
            JArray jData = new JArray(data);
            jData.Insert(0, ev);

            var packet = new Packet() {
                EnginePacketType = EngineIOPacketType.MESSAGE,
                SocketPacketType = SocketIOPacketType.EVENT,
                _data = jData,
                ID = id
            };

            packet.ExtractBinaryData();
            
            if(packet.numAttachments > 0) {
                packet.SocketPacketType = SocketIOPacketType.BINARY_EVENT;
            }

            return packet;
        }

        public static Packet Ack(int id, object[] data) {
            JArray jData = new JArray(data);

            var packet = new Packet() {
                EnginePacketType = EngineIOPacketType.MESSAGE,
                SocketPacketType = SocketIOPacketType.ACK,
                _data = jData,
                ID = id
            };

            packet.ExtractBinaryData();

            if(packet.numAttachments > 0) {
                packet.SocketPacketType = SocketIOPacketType.BINARY_ACK;
            }

            return packet;
        }

        public static byte[] BinaryPacket(byte[] data) {
            byte[] binaryPacket = new byte[data.Length + 1];
            data.CopyTo(binaryPacket, 1);
            binaryPacket[0] = (byte)EngineIOPacketType.MESSAGE;
            //Debug.Log("Packet.cs: BinaryPacket: data length: " + data.Length + " data: " + data[0] + " " + data[1] + " " + data[2] + " " + data[3] + " binaryPacket length: " + binaryPacket.Length + " binaryPacket: " + binaryPacket[0] + " " + binaryPacket[1] + " " + binaryPacket[2] + " " + binaryPacket[3]);
            return binaryPacket;
        }

        void ExtractBinaryData() {
            //Debug.Log("Packet.cs: ExtractBinaryData: _data: " + _data);
            ReplaceData(_data, (token) => {
                if (token.Type == JTokenType.Bytes) {
                    var placeholder = new BinaryPlaceholder() {
                        _placeholder = true,
                        num = Attachments.Count
                    };
                    Attachments.Add(token.ToObject<byte[]>());
                    return JObject.FromObject(placeholder);
                }

                return null;
            });

            numAttachments = Attachments.Count;
        }

        static void ReplaceData(JToken node, Func<JToken, JToken> getReplacement) {
            if (node.Type == JTokenType.Object) {
                var obj = (JObject)node;
                foreach (var prop in obj.Properties()) {
                    var replacement = getReplacement(obj[prop.Name]);
                    if (replacement != null) {
                        obj[prop.Name] = replacement;
                    } else {
                        ReplaceData(obj[prop.Name], getReplacement);
                    }
                }
            } else if (node.Type == JTokenType.Array) {
                var arr = (JArray)node;
                for (int i = 0; i < arr.Count; i++) {
                    var replacement = getReplacement(arr[i]);
                    if (replacement != null) {
                        arr[i] = replacement;
                    } else {
                        ReplaceData(arr[i], getReplacement);
                    }
                }
            }
        }

        public string Encode() {
            var packetStringBuilder = new StringBuilder();

            //engineIO packet type
            packetStringBuilder.Append((int)EnginePacketType);

            //if it isn't a message packet we're done
            if (EnginePacketType != EngineIOPacketType.MESSAGE) return packetStringBuilder.ToString();

            //socketIO packet type
            packetStringBuilder.Append((int)SocketPacketType);

            //if type is binary append the number of attachments followed by a dash (-)
            if(
                SocketPacketType == SocketIOPacketType.BINARY_EVENT ||
                SocketPacketType == SocketIOPacketType.BINARY_ACK
            ) {
                packetStringBuilder.Append(numAttachments);
                packetStringBuilder.Append("-");
            }

            //if the namespace isn't default ("/") or empty append it followed by a comma (,)
            if (Nsp != "" && Nsp != "/") {
                packetStringBuilder.Append(Nsp);
                packetStringBuilder.Append(",");
            }

            //if the packet has an id (>= 0) append it
            if(ID >= 0) {
                packetStringBuilder.Append(ID);
            }

            //if the packet contains data append it
            if(_data.Count() > 0) {
                packetStringBuilder.Append(_data.ToString());
            }

            return packetStringBuilder.ToString();
        }

        public static Packet Parse(string packetData) {
            try {
                int parserIndex = 1;
                var packet = new Packet();

                //parse packet types
                packet.EnginePacketType = (EngineIOPacketType)int.Parse(packetData[0].ToString());
                if (packet.EnginePacketType == EngineIOPacketType.MESSAGE) {
                    packet.SocketPacketType = (SocketIOPacketType)int.Parse(packetData[1].ToString());
                    parserIndex++;
                }

                //done?
                if (parserIndex >= packetData.Length) return packet;

                //Debug.Log("Packet.cs: Parse: packet.SocketPacketType: " + packet.SocketPacketType + " parserIndex: " + parserIndex + " packetData.Length: " + packetData.Length + " left: " + (packetData.Length - parserIndex));

                //parse numAttachments if type binary
                if (
                    packet.SocketPacketType == SocketIOPacketType.BINARY_EVENT ||
                    packet.SocketPacketType == SocketIOPacketType.BINARY_ACK
                ) {
                    var numAttachmentsBuilder = new StringBuilder();
                    while (
                        parserIndex < packetData.Length &&
                        packetData[parserIndex] != '-'
                    ) {
                        numAttachmentsBuilder.Append(packetData[parserIndex]);
                        parserIndex++;
                    }
                    packet.numAttachments = int.Parse(numAttachmentsBuilder.ToString());
                    parserIndex++;
                    //Debug.Log("Packet.cs: Parse: numAttachments: " + packet.numAttachments + " parserIndex: " + parserIndex + " packetData.Length: " + packetData.Length + " left: " + (packetData.Length - parserIndex));

                }

                //done?
                if (parserIndex >= packetData.Length) return packet;

                //parse namespace
                if (packetData[parserIndex] == '/') {
                    var nspBuilder = new StringBuilder();
                    while (
                        parserIndex < packetData.Length &&
                        packetData[parserIndex] != ','
                    ) {
                        nspBuilder.Append(packetData[parserIndex]);
                        parserIndex++;
                    }
                    packet.Nsp = nspBuilder.ToString();
                    parserIndex++;
                }

                //done?
                if (parserIndex >= packetData.Length) return packet;

                //parse id
                if (char.IsNumber(packetData[parserIndex])) {
                    var idBuilder = new StringBuilder();
                    while (
                        parserIndex < packetData.Length &&
                        char.IsNumber(packetData[parserIndex])
                    ) {
                        idBuilder.Append(packetData[parserIndex]);
                        parserIndex++;
                    }
                    packet.ID = int.Parse(idBuilder.ToString());
                }

                //done?
                if (parserIndex >= packetData.Length) return packet;

                //parse message data
                packet._data = (JToken)JsonConvert.DeserializeObject(packetData.Substring(parserIndex));

                return packet;
            }catch(Exception e) {
                throw new SocketIOException("Failed to parse packet: " + packetData, e);
            }
        }
    }
}

#endif
