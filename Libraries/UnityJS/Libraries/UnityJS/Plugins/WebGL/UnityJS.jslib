/*
* UnityJS.jslib
* Unity3D / JavaScript Bridge
* Don Hopkins, Ground Up Software.
*/


mergeInto(LibraryManager.library, {


    // Called by Unity when awakened.
    _UnityJS_HandleAwake: function _UnityJS_HandleAwake(allocateTextureCallback, freeTextureCallback, lockTextureCallback, unlockTextureCallback, allocateDataCallback, freeDataCallback, lockDataCallback, unlockDataCallback)
    {
        //console.log("UnityJS.jslib: _UnityJS_HandleAwake: allocateTextureCallback: " + allocateTextureCallback + " freeTextureCallback: " + freeTextureCallback + " lockTextureCallback: " + lockTextureCallback + " unlockTextureCallback: " + unlockTextureCallback + allocateDataCallback: " + allocateDataCallback + " freeDataCallback: " + freeDataCallback + " lockDataCallback: " + lockDataCallback + " unlockDataCallback: " + unlockDataCallback);

        if (!window.bridge) {
            window.bridge = new UnityJSBridge();
        }

        if (!window.bridge._UnityJS_JSToUnityEventQueue) {
            window.bridge._UnityJS_JSToUnityEventQueue = [];
        }

        if (!window.bridge._UnityJS_UnityToJSEventQueue) {
            window.bridge._UnityJS_UnityToJSEventQueue = [];
        }

        // Called by JS to queue events to Unity.
        function _UnityJS_SendJSToUnityEvents (evListString) {
            window.bridge._UnityJS_JSToUnityEventQueue.push(evListString);
        }

        window.bridge._UnityJS_SendJSToUnityEvents = _UnityJS_SendJSToUnityEvents;

        function _UnityJS_AllocateTexture(width, height)
        {
            //console.log("UnityJS.jslib: _UnityJS_AllocateTexture: width: " + width + " height: " + height + " allocateTextureCallback: " + allocateTextureCallback);
            var result = Runtime.dynCall('iii', allocateTextureCallback, [width, height]);
            //console.log("UnityJS.jslib: _UnityJS_AllocateTexture: result: " + result);
            return result;
        };
        window.bridge._UnityJS_AllocateTexture = _UnityJS_AllocateTexture;

        function _UnityJS_FreeTexture(id)
        {
            //console.log("UnityJS.jslib: _UnityJS_FreeTexture: id: " + id + " freeTextureCallback: " + freeTextureCallback);
            Runtime.dynCall('vi', freeTextureCallback, [id]);
        }
        window.bridge._UnityJS_FreeTexture = _UnityJS_FreeTexture;

        function _UnityJS_LockTexture(id)
        {
            //console.log("UnityJS.jslib: _UnityJS_LockTexture: id: " + id + " lockTextureCallback: " + lockTextureCallback);
            var result = Runtime.dynCall('ii', lockTextureCallback, [id]);
            //console.log("UnityJS.jslib: _UnityJS_LockTexture: result: " + result);
            return result;
        }
        window.bridge._UnityJS_LockTexture = _UnityJS_LockTexture;

        function _UnityJS_UnlockTexture(id)
        {
            //console.log("UnityJS.jslib: _UnityJS_UnlockTexture: id: " + id + " unlockTextureCallback: " + unlockTextureCallback);
            Runtime.dynCall('vi', unlockTextureCallback, [id]);
        }
        window.bridge._UnityJS_UnlockTexture = _UnityJS_UnlockTexture;

        function _UnityJS_UpdateTexture(id, imageData)
        {
            //console.log("UnityJS.jslib: _UnityJS_UpdateTexture: id: " + id + " imageData: " + imageData + " width: " + imageData.width + " height: " + imageData.height + " data: " + imageData.data);
            var pointer = _UnityJS_LockTexture(id);
            var byteCount = imageData.width * imageData.height * 4;
            var heapBytes = new Uint8Array(Module.HEAPU8.buffer, pointer, byteCount);
            //console.log("UnityJS.jslib: _UnityJS_UpdateTexture: pointer: " + pointer + " byteCount: " + byteCount + " buffer: " + buffer + " heapBytes: " + heapBytes);
            heapBytes.set(imageData.data);
            _UnityJS_UnlockTexture(id);
            //console.log("UnityJS.jslib: _UnityJS_UpdateTexture: done");
        }
        window.bridge._UnityJS_UpdateTexture = _UnityJS_UpdateTexture;

        function _UnityJS_AllocateData(size)
        {
            //console.log("UnityJS.jslib: _UnityJS_AllocateData: size: " + size + " allocateDataCallback: " + allocateDataCallback);
            var result = Runtime.dynCall('ii', allocateDataCallback, [size]);
            //console.log("UnityJS.jslib: _UnityJS_AllocateData: result: " + result);
            return result;
        };
        window.bridge._UnityJS_AllocateData = _UnityJS_AllocateData;

        function _UnityJS_FreeData(id)
        {
            //console.log("UnityJS.jslib: _UnityJS_FreeData: id: " + id + " freeDataCallback: " + freeDataCallback);
            Runtime.dynCall('vi', freeDataCallback, [id]);
        }
        window.bridge._UnityJS_FreeData = _UnityJS_FreeData;

        function _UnityJS_LockData(id)
        {
            //console.log("UnityJS.jslib: _UnityJS_LockData: id: " + id + " lockDataCallback: " + lockDataCallback);
            var result = Runtime.dynCall('ii', lockDataCallback, [id]);
            //console.log("UnityJS.jslib: _UnityJS_LockData: result: " + result);
            return result;
        }
        window.bridge._UnityJS_LockData = _UnityJS_LockData;

        function _UnityJS_UnlockData(id)
        {
            //console.log("UnityJS.jslib: _UnityJS_UnlockData: id: " + id + " unlockDataCallback: " + unlockDataCallback);
            Runtime.dynCall('vi', unlockDataCallback, [id]);
        }
        window.bridge._UnityJS_UnlockData = _UnityJS_UnlockData;

        function _UnityJS_UpdateData(id, data)
        {
            //console.log("UnityJS.jslib: _UnityJS_UpdateData: id: " + id + " data: " + data + " length: " + data.length);
            var pointer = _UnityJS_LockData(id);
            var byteCount = data.length;
            var heapBytes = new Uint8Array(Module.HEAPU8.buffer, pointer, byteCount);
            //console.log("UnityJS.jslib: _UnityJS_UpdateData: pointer: " + pointer + " byteCount: " + byteCount + " buffer: " + buffer + " heapBytes: " + heapBytes);
            heapBytes.set(data);
            _UnityJS_UnlockData(id);
            //console.log("UnityJS.jslib: _UnityJS_UpdateData: done");
        }
        window.bridge._UnityJS_UpdateData = _UnityJS_UpdateData;

    },


    // Called by Unity when destroyed.
    _UnityJS_HandleDestroy: function _UnityJS_HandleDestroy()
    {
        //console.log("UnityJS.jslib: _UnityJS_HandleDestroy");
    },


    // Called by Unity to evaluate JS code.
    _UnityJS_EvaluateJS: function _UnityJS_EvaluateJS(jsPointer)
    {
        var js = Pointer_stringify(jsPointer);
        //console.log("UnityJS.jslib: _UnityJS_EvaluateJS: js:", js);
        bridge.evaluateJS(js);
    },


    // Called by Unity to receive all events from JS.
    _UnityJS_ReceiveJSToUnityEvents: function _UnityJS_ReceiveJSToUnityEvents()
    {
        var eventCount = window.bridge._UnityJS_JSToUnityEventQueue.length;
        if (eventCount == 0) {
            return null;
        }

        var str =
            window.bridge._UnityJS_JSToUnityEventQueue.join(',');

        window.bridge._UnityJS_JSToUnityEventQueue.splice(0, eventCount);

        var bufferSize = lengthBytesUTF8(str) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(str, buffer, bufferSize);

        return buffer;
    },


    // Called by Unity to queue events to JS.
    _UnityJS_SendUnityToJSEvents: function _UnityJS_SendUnityToJSEvents(evListStringPointer)
    {
        var evListString = Pointer_stringify(evListStringPointer);
        window.bridge._UnityJS_UnityToJSEventQueue.push(evListString);
    },

    // Called by Unity to distribute queued events from Unity to JS.
    _UnityJS_DistributeJSEvents: function _UnityJS_DistributeJSEvents()
    {
        var evList = null;
        var evListStringLength = 0;
        var eventCount = window.bridge._UnityJS_UnityToJSEventQueue.length;

        var evListString = null;
        if (eventCount) {
            evListString = window.bridge._UnityJS_UnityToJSEventQueue.join(',');
            window.bridge._UnityJS_UnityToJSEventQueue.splice(0, eventCount);
        }

        if (evListString) {
            var json = "[" + evListString + "]";
            evListStringLength = json.length;
            evList = JSON.parse(json);
        }

        window.bridge.distributeEvents(evList, evListStringLength);
    }


});
