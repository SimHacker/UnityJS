////////////////////////////////////////////////////////////////////////
// WebGLAce.jslib
// Don Hopkins, Ground Up Software.


mergeInto(LibraryManager.library, {


    _createEditor: function(x, y, width, height, text, configScript) {
        text = Pointer_stringify(text);
        configScript = Pointer_stringify(configScript);

        return WebGLAce._createEditor(x, y, width, height, text, configScript);
    },


    _destroyEditor: function(id) {
        WebGLAce._destroyEditor(id);
    },


    _resizeEditor: function(id, x, y, width, height) {
        WebGLAce._resizeEditor(id, x, y, width, height);
    },


    _setEditorVisible: function(id, visible) {
        WebGLAce._setEditorVisible(id, visible);
    },


    _setEditorFocused: function(id, focused) {
        WebGLAce._setEditorFocused(id, focused);
    },


    _setEditorReadOnly: function(id, readOnly) {
        WebGLAce._setEditorReadOnly(id, readOnly);
    },


    _setEditorText: function(id, text) {
        text = Pointer_stringify(text);

        WebGLAce._setEditorText(id, text);
    },


    _getEditorText: function(id) {
        var result = WebGLAce._getEditorText(id);

        var size = lengthBytesUTF8(result) + 1;
        var buffer = _malloc(size);
        stringToUTF8(result, buffer, size);

        return buffer;
    },


    _configureEditor: function(id, configScript) {
        configScript = Pointer_stringify(configScript);

        WebGLAce._configureEditor(id, configScript);
    },


});


////////////////////////////////////////////////////////////////////////
