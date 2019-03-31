////////////////////////////////////////////////////////////////////////
// WebGLAce.jss
// Don Hopkins, Ground Up Software.


class WebGLAce {


    // Class methods.


    // Returns int id of new editor.
    static _createEditor(x, y, width, height, text, configScript)
    {

        var id = WebGLAce.nextEditorID++;
        var editorID = "ace_editor_" + id;

        var editorElement = document.createElement('pre');

        editorElement.id = editorID;

        editorElement.style = 
            'position: relative; ' +
            'display: block; ' +
            'left: ' + x + 'px; ' +
            'top: ' + y + 'px; ' +
            'width: ' + width + 'px; ' +
            'height: ' + height + 'px; ' +
            'margin: 0; ' +
            'border: 0; ' +
            'padding: 0;'

        document.body.appendChild(editorElement);

        // Create Ace editor.
        var editor = ace.edit(editorID);
        WebGLAce.editors[id] = editor;
        WebGLAce.editorElements[id] = editorElement;

        editor.setTheme("ace/theme/chrome");
        editor.session.setMode("ace/mode/javascript");
        editor.session.setValue(text); // resets undo history

        editor.on("focus", (e) => {
            console.log("WebGLAce: editor: focus: e:", e);
        });

        editor.on("blur", (e) => {
            console.log("WebGLAce: editor: blur: e:", e);
        });

        editor.on("change", (e) => {
            console.log("WebGLAce: editor: change: e:", e);
        });

        if (configScript) {
            eval(configScript);
        }

        return id;
    }


    static _destroyEditor(id)
    {
        var editor = WebGLAce.editors[id];
        var editorElement = WebGLAce.editorElements[id];
        if (!editor) {
            console.log("WebGLAce: _destroyEditor: unknown id:", id);
            return;
        }

        editor.destroy();

        delete WebGLAce.editors[id];
        delete WebGLAce.editorElements[id];

        editorElement.parentElement.removeChild(editorElement);
    }


    static _resizeEditor(id, x, y, width, height)
    {
        var editor = WebGLAce.editors[id];
        var editorElement = WebGLAce.editorElements[id];
        if (!editor) {
            console.log("WebGLAce: _resizeEditor: unknown id:", id);
            return;
        }

        editorElement.style.left = x + 'px;';
        editorElement.style.top = y + 'px;';
        editorElement.style.width = width + 'px;';
        editorElement.style.height = height + 'px;';

        // TODO
        editor.resize()
    }


    static _setEditorVisible(id, visible)
    {
        var editor = WebGLAce.editors[id];
        var editorElement = WebGLAce.editorElements[id];
        if (!editor) {
            console.log("WebGLAce: _setEditorVisible: unknown id:", id);
            return;
        }

        editorElement.style.display = visible ? 'block' : 'none';
    }


    static _setEditorFocused(id, focused)
    {
        var editor = WebGLAce.editors[id];
        var editorElement = WebGLAce.editorElements[id];
        if (!editor) {
            console.log("WebGLAce: _setEditorFocused: unknown id:", id);
            return;
        }

        if (focused) {
            editor.focus();
        } else {
            editor.blur();
        }
    }


    static _setEditorReadOnly(id, readOnly)
    {
        var editor = WebGLAce.editors[id];
        var editorElement = WebGLAce.editorElements[id];
        if (!editor) {
            console.log("WebGLAce: _setEditorReadOnly: unknown id:", id);
            return;
        }

        editor.setReadOnly(readOnly);
    }


    static _setEditorText(id, text)
    {
        var editor = WebGLAce.editors[id];
        if (!editor) {
            console.log("WebGLAce: _setEditorText: unknown id:", id);
            return;
        }

        editor.session.setValue(text); // resets undo history
    }


    static _getEditorText(id)
    {
        var editor = WebGLAce.editors[id];
        if (!editor) {
            console.log("WebGLAce: _getEditorText: unknown id:", id);
            return '';
        }

        var text = editor.getValue();
        return text;
    }


    static _configureEditor(id, configScript)
    {
        var editor = WebGLAce.editors[id];
        var editorElement = WebGLAce.editorElements[id];
        if (!editor) {
            console.log("WebGLAce: _configureEditor: unknown id:", id);
            return;
        }

        eval(configScript);
    }


};


// Class variables.


WebGLAce.nextEditorID = 0;
WebGLAce.editors = {};
WebGLAce.editorElements = {};


////////////////////////////////////////////////////////////////////////
