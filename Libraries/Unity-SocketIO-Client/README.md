# Unity SocketIO Client
A SocketIO client for Unity written in C#.
It's compatible with the latest version of the SocketIO server for node.js (2.0.2) and should be compatible with any other version that leverages the same protocol spec.

## Features
- Send/Receive SocketIO events
- Send/Receive event callbacks
- Send/Receive binary data

## Basic Usage
### Importing the Namespace
Everything is contained within the namespace `Dpoch.SocketIO`
```cs
    using Dpoch.SocketIO;
```
### Connecting to a SocketIO Server
###### Client (C#)
```cs
    var socket = new SocketIO("ws://127.0.0.1:80/socket.io/?EIO=4&transport=websocket");
    
    socket.OnOpen += () => Debug.Log("Socket open!");
    socket.OnConnectFailed += () => Debug.Log("Socket failed to connect!");
    socket.OnClose += () => Debug.Log("Socket closed!");
    socket.OnError += (err) => Debug.Log("Socket Error: " + err);
    
    socket.Connect();
```
__NOTE: The URL has to be the full SocketIO websocket url. The `/socket.io/?EIO=4&transport=websocket` part is required. Websockets are the only supported transport.__

### Sending/Receiving Events

###### Client (C#)
```cs
    socket.OnOpen += () => {
        socket.Emit("client-handshake");
        Debug.Log("Sent client handshake");
    };
    
    socket.On("server-handshake", (ev) => {
        Debug.Log("Received server handshake");
    });
    
    socket.Connect();
```

###### Server (JavaScript)
```javascript
    //socket.io server boilerplate
    
    io.on("connection", (socket) => {
        socket.on("client-handshake", () => {
            console.log("Received client handshake");
            
            socket.emit("server-handshake");
            console.log("Sent server handshake");
        });
    });
```

### Sending/Receiving Events with Data

##### Sending Data
You can optionally send or receive data with your events. Any data you send from the client to the server will automatically get serialized into JSON. This library uses the popular JSON-library JSON.NET for JSON serialization. There are a number of ways you can specify the serialization behaviour for your Classes. For more information please consult the [JSON.NET Documentation](http://www.newtonsoft.com/json/help/html/SerializationAttributes.htm).

###### Client (C#)
```cs
    socket.Emit("client-handshake", "This string will get sent to the server");
    
    socket.Emit("multiple-args", "You can also send multiple values", "like so", 42f);
    
	/*
    class MyCustomJsonClass{
        public string myMember;
    }
    */

    var myData = new MyCustomJsonClass(){
        myMember = "This member will get serialized"
    };
    
    socket.Emit("json-object", myData);
```

###### Server (JavaScript)
```javascript
    //socket.io server boilerplate
    
    io.on("connection", (socket) => {
        socket.on("client-handshake", (data) => {
            console.log(data); // output: "This string will get sent to the server"
        });
        
        socket.on("multiple-args", (arg0, arg1, arg2) => {
            console.log(arg0); //output: "You can also send multiple values"
            console.log(arg1); //output: "like so"
            console.log(arg2); //output: 42
        });
        
        socket.on("json-object", (data) => {
            console.log(data.myMember); //output: "This member will get serialized"
        });
    });
```

##### Receiving Data
The `Data` property of the `SocketIOEvent` object that gets passed to the event handler is a `JArray` which is part of JSON.NET. It can be used like a normal Array. It contains `JToken`s and the elements correspond to the parameters that got passed to the server emit. You can use the `JToken`s to extract the data that you're interested in or simply parse it into an object. For more information please consult the [JSON.NET Documentation](http://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JToken.htm).

###### Client (C#)
```cs
	/*
    class MyCustomJsonClass{
        public string myMember;
    }
	*/
    
    socket.On("some-data", (ev) => {
        string myString = ev.Data[0].ToObject<string>();
        float myNumber = ev.Data[1].ToObject<float>();
        MyCustomJsonClass myJsonObj = ev.Data[2].ToObject<MyCustomJsonClass>();
        
        Debug.Log(myString); //output: "My string"
        Debug.Log(myNumber); //output: 42
        Debug.Log(myJsonObj.myMember); //output: "My member string"
    });
```

###### Server (JavaScript)
```javascript
    //socket.io server boilerplate
    
    io.on("connection", (socket) => {
        socket.emit("some-data", "My string", 42, { myMember : "My member string" });
    });
```

##### Binary Data / Raw JSON
Any `byte[]` will not get serialized into JSON but sent as a binary attachment. This happens automatically so you don't have to worry about it. On the server the `byte[]` will be available as a JS `Buffer`. Conversely if you're sending a `Buffer` to the client it will be a `byte[]`.

If you want to send a raw JSON string as JSON data you can use the `JRaw` Class which is part of the `Newtonsoft.Json.Linq` namespace. For more information please consult the [JSON.NET Documentation](http://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JRaw.htm).

### Sending/Receiving Event Callbacks
Sometimes you might want to send or receive an acknowlegement that the client or server received/processed an event. These acknowledgements are part of the SocketIO spec and you can handle them with this library.
Note that events can only be acknowledged once and that it won't work with broadcasts.
To check whether you can acknowledge an event you can use the `IsAcknowledgable` property of the `SocketIOEvent` object.

##### Sending Acknowledgements
###### Client (C#)
```cs
    socket.On("some-event", (ev) => {
        Debug.Log(ev.Data[0].ToObject<string>()); //output: "optional event data"
        if(ev.IsAcknowledgable) ev.Acknowledge("optional acknowledgement data");
    });
```

###### Server (JavaScript)
```javascript
    //socket.io server boilerplate
    
    io.on("connection", (socket) => {
        socket.emit("some-event", "optional event data", (ackData) => {
            console.log(ackData); //output: "optional acknowledgement data"
            console.log("some-event acknowledged");
        });
    });
```

##### Receiving Acknowledgements
###### Client (C#)
```cs
    socket.Emit(
        "some-event", 
        (JArray ackData) => {
            console.log(ackData[0].ToObject<string>()); //output: "optional acknowlegdement data"
            Debug.Log("some-event acknowledged");
        },
        "optional event data"
    );
```

###### Server (JavaScript)
```javascript
    //socket.io server boilerplate
    
    io.on("connection", (socket) => {
        socket.on("some-event", (arg0, acknowledge) => {
            console.log(arg0); //output: "optional event data"
            acknowledge("optional acknowledgement data");
        });
    });
```

