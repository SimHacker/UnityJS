////////////////////////////////////////////////////////////////////////
// bridge-transport-socketio.js
// Unity3D / JavaScript Bridge
// Don Hopkins, Ground Up Software.


////////////////////////////////////////////////////////////////////////


function InitializeBridgeSocketIO()
{
    window.bridge = window.bridge || new UnityJSBridge();

    //var url = 'ws://localhost:3000/socket.io/?EIO=4&transport=websocket';
    var url = null;
    var socket = window.bridge._UnityJS_Socket = io(url);
    var friends = [];
    var displayEngine = null;

    socket.on('AddFriend', (message) => {
        var id = message.id;
        var engineType = message.engineType;
        var friend = {
            id: id,
            engineType: engineType
        };
        console.log('AddFriend', 'id:', id, 'engineType:', engineType);
        friends.push(friend);

        switch (engineType) {

            case 'DisplayEngine':
                if (!displayEngine) {
                    displayEngine = friend;
                    console.log("AddFriend: connected to displayEngine: " + JSON.stringify(displayEngine));
                } else {
                    console.log("AddFriend: already have a displayEngine:", JSON.stringify(displayEngine), "friend:", JSON.stringify(friend));
                }
                break;

            default:
                console.log("AddFriend: unexpected engineType:", engineType);
                break;

        }

    });

    socket.on('RemoveFriend', (message) => {
        var id = message.id;
        var engineType = message.engineType;
        console.log('RemoveFriend', 'id:', id, 'engineType:', engineType);
        friends.some((friend, friendIndex) => {
            if (friend.id == id) {
                friends.splice(friendIndex, 1);

                switch (engineType) {

                    case 'DisplayEngine':
                        if (displayEngine) {
                            console.log("RemoveFriend: disconnected to displayEngine: " + JSON.stringify(displayEngine));
                            displayEngine = null;
                        } else {
                            console.log("RemoveFriend: already disconnected from displayEngine: friend: " + JSON.stringify(friend));
                        }
                        break;

                    default:
                        console.log("RemoveFriend: unexpected engineType:", engineType);
                        break;

                }

                return true;
            }
            return false;
        });
    });

    socket.on('EvaluateJS', (message) => {
        //console.log('EvaluateJS', message);
        window.bridge.evaluateJS(message);
    });

    socket.emit('Hello', {
        engineType: 'ScriptingEngine'
    });

}


////////////////////////////////////////////////////////////////////////
