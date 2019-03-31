////////////////////////////////////////////////////////////////////////
// WebGLAce.cs
// Don Hopkins, Ground Up Software.


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


public class WebGLAce
{


#if UNITY_WEBGL && !UNITY_EDITOR

    [DllImport("__Internal")]
    private static extern int _createEditor(int x, int y, int width, int height, string text, string configScript);

    [DllImport("__Internal")]
    private static extern void _destroyEditor(int id);

    [DllImport("__Internal")]
    private static extern void _resizeEditor(int id, int x, int y, int width, int height);

    [DllImport("__Internal")]
    private static extern void _setEditorVisible(int id, bool visible);

    [DllImport("__Internal")]
    private static extern void _setEditorFocused(int id, bool focused);

    [DllImport("__Internal")]
    private static extern void _setEditorReadOnly(int id, bool readOnly);

    [DllImport("__Internal")]
    private static extern void _setEditorText(int id, string text);

    [DllImport("__Internal")]
    private static extern string _getEditorText(int id);

    [DllImport("__Internal")]
    private static extern string _configureEditor(int id, string configScript);

#else

    static int nextEditorID = 0;
    static Dictionary<int, string> editorIDToText = new Dictionary<int, string>();
#endif


    public static int CreateEditor(int x, int y, int width, int height, string text, string configScript)
    {
        Debug.Log("WebGLAce: CreateEditor: x: " + x + " y: " + y + " width: " + width + " height: " + height + " text: " + text + " configScript: " + configScript);
#if UNITY_WEBGL && !UNITY_EDITOR
        return _createEditor(x, y, width, height, text, configScript);
#else
        editorIDToText[nextEditorID] = text;
        return nextEditorID++;
#endif
    }


    public static void DestroyEditor(int id)
    {
        Debug.Log("WebGLAce: DestroyEditor: id: " + id);
#if UNITY_WEBGL && !UNITY_EDITOR
        _destroyEditor(id);
#else
        editorIDToText.Remove(id);
#endif
    }


    public static void ResizeEditor(int id, int x, int y, int width, int height)
    {
        Debug.Log("WebGLAce: ResizeEditor: id: " + id + " x: " + x + " y: " + y + " width: " + width + " height: " + height);
#if UNITY_WEBGL && !UNITY_EDITOR
        _resizeEditor(id, x, y, width, height);
#endif
    }


    public static void SetEditorVisible(int id, bool visible)
    {
        Debug.Log("WebGLAce: SetEditorVisible: id: " + id + " visible: " + visible);
#if UNITY_WEBGL && !UNITY_EDITOR
        _setEditorVisible(id, visible);
#endif
    }


    public static void SetEditorFocused(int id, bool focused)
    {
        Debug.Log("WebGLAce: SetEditorFocused: id: " + id + " focused: " + focused);
#if UNITY_WEBGL && !UNITY_EDITOR
        _setEditorFocused(id, focused);
#endif
    }


    public static void SetEditorReadOnly(int id, bool readOnly)
    {
        Debug.Log("WebGLAce: SetEditorReadOnly: id: " + id + " readOnly: " + readOnly);
#if UNITY_WEBGL && !UNITY_EDITOR
        _setEditorReadOnly(id, readOnly);
#endif
    }


    public static void SetEditorText(int id, string text)
    {
        Debug.Log("WebGLAce: SetEditorText: id: " + id + " text: " + text);
#if UNITY_WEBGL && !UNITY_EDITOR
        _setEditorText(id, text);
#else
        editorIDToText[id] = text;
#endif
    }


    public static string GetEditorText(int id)
    {
        string text;
#if UNITY_WEBGL && !UNITY_EDITOR
        text = _getEditorText(id);
#else
        text = editorIDToText.ContainsKey(id) ? editorIDToText[id] : "";
#endif
        Debug.Log("WebGLAce: GetEditorText: id: " + id + " text: " + text);
        return text;
    }


    public static void ConfigureEditor(int id, string configScript)
    {
        Debug.Log("WebGLAce: ConfigureEditor: id: " + id + " configScript: " + configScript);
#if UNITY_WEBGL && !UNITY_EDITOR
        _configureEditor(id, configScript);
#endif
    }


}


////////////////////////////////////////////////////////////////////////
