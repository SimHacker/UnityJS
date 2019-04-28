////////////////////////////////////////////////////////////////////////
// server.js
//
// UnityJS BridgeTransportSocketIO Server for node.js.
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


////////////////////////////////////////////////////////////////////////
// Requirements


var express = require('express');
var cookieParser = require('cookie-parser');
var session = require('express-session');
var http = require('http');
var socketio = require('socket.io');


////////////////////////////////////////////////////////////////////////
// Constants


var port = process.env.PORT || 3000;
var secret = '568c3c9jgwyx8vis';
var staticDirectory = 'static';
var indexFileName = '/static/index.html';
var pingInterval = 10000;
var pingTimeout = 5000;


////////////////////////////////////////////////////////////////////////
// Globals


var engines = [];


////////////////////////////////////////////////////////////////////////
// Servers


var app = express();
app.use(cookieParser());
app.use(session({secret: secret}));
app.use(express.json());
app.use(express.urlencoded());
app.use(express.static(staticDirectory));

var httpServer = http.Server(app);

var io = socketio(httpServer, {
    pingInterval: pingInterval,
    pingTimeout: pingTimeout,
    binary: true,
    cookie: false
});


////////////////////////////////////////////////////////////////////////
// HTTP Handlers


app.get('/', function(req, res) {

    //console.log("__dirname:", __dirname, "app:", req.app, "baseUrl:", req.baseUrl, "body:", req.body, "cookies:", req.cookies, "hostname:", req.hostname, "ip:", req.ip, "method:", req.method, "params:", req.params, "path:", req.path, "protocol:", req.protocol, "query:", req.query, "secure:", req.secure, "signedCookies:", req.signedCookies, "xhr:", req.xhr);
    //console.log('get /index.html');

    res.sendFile(__dirname + indexFileName);

});


////////////////////////////////////////////////////////////////////////
// SocketIO Handlers


io.on('connection', function(socket) {

    console.log('connection:', 'socket.id:', socket.id);

    socket.on('disconnect', function (reason) {
        console.log('disconnect:', 'socket.id:', socket.id, 'reason:', reason);

        StopEngine(socket);
    });

    // Chat: *Engine => server => *Engine
    socket.on('Chat', function(message) {
        console.log('Chat:', 'socket.id:', socket.id, 'message:', message);

        io.emit('Chat', {
            id: socket.id,
            message: message
        });
    });

    // Hello: *Engine => server
    socket.on('Hello', function(message) {
        console.log('Hello:', 'socket.id:', socket.id, 'message:', message);

        var engineType = message['engineType'];
        if (!engineType) {
            console.log('ERROR: Hello: missing engineType!', 'socket.id:', socket.id, 'message:', message);
            return;
        }

        StartEngine(socket, engineType);
    });

    // EvaluateJS: DisplayEngine => server => ScriptingEngine
    socket.on('EvaluateJS', function(message) {
        console.log('EvaluateJS:', 'socket.id:', socket.id, 'message:', message);

        var scriptingEngine = socket.friendTypes.ScriptingEngine;
        if (scriptingEngine == null) {
            console.log('EvaluateJS: no ScriptingEngine', 'socket.id:', socket.id);
            return;
        }

        scriptingEngine.emit('EvaluateJS', message);
    });

    // SendEventList: ScriptingEngine => server => DisplayEngine
    socket.on('SendEventList', function(evListString) {
        //console.log('SendEventList:', 'socket.id:', socket.id, 'message:', evListString);

        var displayEngine = socket.friendTypes.DisplayEngine;
        if (displayEngine == null) {
            console.log('SendEventList: no DisplayEngine', 'socket.id:', socket.id);
            return;
        }

        displayEngine.emit('SendEventList', evListString);
    });

    // SendBlob: ScriptingEngine => DisplayEngine
    socket.on('SendBlob', function(blobID, blob) {
        console.log('SendBlob:', 'socket.id:', socket.id, 'blobID:', blobID, 'blob type:', typeof blob, "length:", blob.length, "data:", blob[0], blob[1], blob[2], blob[3]);

        var displayEngine = socket.friendTypes.DisplayEngine;
        if (displayEngine == null) {
            console.log('SendBlob: no DisplayEngine', 'socket.id:', socket.id);
            return;
        }

        displayEngine.binary(true).emit('SendBlob', blobID, blob);
    });

});


////////////////////////////////////////////////////////////////////////
// Utilities


function StartEngine(engine, engineType)
{
    engine.engineType = engineType;
    engine.friends = [];
    engine.friendTypes = {};

    engines.push(engine);
    console.log("StartEngine:", "engines:", engines.length);

    switch (engineType) {

        case 'ScriptingEngine':
            IntroduceEngines(engine, 'DisplayEngine', true);
            break;

        case 'DisplayEngine':
            IntroduceEngines(engine, 'ScriptingEngine', true);
            break;

        default:
            console.log('ERROR: Hello: unexpected engineType:', engineType, 'engine.id:', engine.id);
            return;

    }

}


function StopEngine(engine)
{
    console.log("StopEngine:", "engines:", engines.length);

    var i = engines.indexOf(engine);
    if (i < 0) {
        //console.log("StopEngine: missing engine:", engine);
        return;
    }

    engines.splice(i, 1);

    engines.forEach(function (otherEngine) {
        var i = otherEngine.friends.indexOf(engine);
        if (i < 0) {
            return;
        }
        otherEngine.friends.splice(i, 1);
        if (otherEngine.friendTypes[engine.engineType] == engine) {
            delete otherEngine.friendTypes[engine.engineType];
        }
        otherEngine.emit('RemoveFriend', {
            id: engine.id, 
            engineType: engine.engineType
        });
    });
}


function IntroduceEngines(engine, otherEngineType, exclusive)
{
    console.log('IntroduceEngines:', engine.engineType, otherEngineType, engine.id);

    var found = false;

    engines.some(function (otherEngine) {

        if ((otherEngine == engine) ||
            (otherEngine.engineType != otherEngineType) ||
            (exclusive &&
             (otherEngine.friendTypes[engine.engineType]))) {
            return false;
        }

        engine.friends.push(otherEngine);
        engine.friendTypes[otherEngine.engineType] = otherEngine;
        otherEngine.friends.push(engine);
        otherEngine.friendTypes[engine.engineType] = engine;
        found = true;

        console.log('IntroduceEngines: found match:', 'exclusive:', exclusive, 'engine:', engine.id, engine.engineType, 'otherEngine:', otherEngine.id, otherEngine.engineType);

        engine.emit('AddFriend', {
            id: otherEngine.id,
            engineType: otherEngine.engineType
        });

        otherEngine.emit('AddFriend', {
            id: engine.id,
            engineType: engine.engineType
        });

        return true;
    });

    if (!found) {
        console.log('IntroduceEngines: no match');
    }
}


////////////////////////////////////////////////////////////////////////
// Start Server


httpServer.listen({
        host: 'localhost',
        port: port
    },
    function() {
        console.log('listening on localhost:' + port);
    });


////////////////////////////////////////////////////////////////////////
