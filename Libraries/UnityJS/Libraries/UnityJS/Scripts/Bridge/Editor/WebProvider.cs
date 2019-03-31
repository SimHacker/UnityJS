////////////////////////////////////////////////////////////////////////
// From:
// https://raw.githubusercontent.com/BenjaminMoore/UnityWebViewInEditor/master/Assets/Editor/WebProvider.cs


#if UNITY_EDITOR && USE_CEF


using System;
using UnityEngine;
using System.Collections;
using System.Reflection;
using UnityEditor;


[InitializeOnLoad]
public static class WebProvider {

    public static BindingFlags bind = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                          BindingFlags.Static;

    //for EditorWindow
    public static Func<object, object> getView;

    //for WebView
    public static Func<ScriptableObject> create; 
    public static InitDelegate init;
    public static Action<object, string> execute;
    public static Action<object, string> loadURL;
    public static Action<object, string> loadFile;
    public static Action<object, string, ScriptableObject> define;
    public static Action<object, ScriptableObject> setDelegate;
    public static Action<object, object> setView;
    public static Action<object, Rect> setRect;
    public static Action<object, bool> setFocus;
    public static Func<object, bool> hasAppFocus;
    public static Action<object, bool> setAppFocus;
    public static Action<object> show;
    public static Action<object> hide;
    public static Action<object> back;
    public static Action<object> forward;
    public static Action<object, string> send;
    public static Action<object> reload;
    public static Action<object, bool> allow;
    public static Action<object> dev;
    public static Action<object> toggle;

    //for callbackWrapper
    public static Action<object, string> callback;

    static WebProvider() {
        var editor = typeof (Editor).Assembly;

        var webViewType = editor.GetType("UnityEditor.WebView");
        create = () => ScriptableObject.CreateInstance(webViewType);

        var parentField = typeof(EditorWindow).GetField("m_Parent", bind);
        getView = inst => parentField.GetValue(inst);


        var initMethod = webViewType.GetMethod("InitWebView");
        init =
            (inst, view, x, y, width, height, handles) => initMethod.Invoke(inst, new[] { view, x, y, width, height, handles});

        var cb = editor.GetType("UnityEditor.WebViewV8CallbackCSharp");
        var cbMethod = cb.GetMethod("Callback");
        callback = (inst, message) => cbMethod.Invoke(inst, new object[] {message});

        var execMethod = webViewType.GetMethod("ExecuteJavascript");
        execute = (inst, code) => execMethod.Invoke(inst, new object[] { code });

        var loadURLMethod = webViewType.GetMethod("LoadURL");
        loadURL = (inst, url) => loadURLMethod.Invoke(inst, new object[] { url });

        var loadFileMethod = webViewType.GetMethod("LoadFile");
        loadFile = (inst, path) => loadFileMethod.Invoke(inst, new object[] { path });
        
        var defineMethod = webViewType.GetMethod("DefineScriptObject");
        define = (inst, path, obj) => defineMethod.Invoke(inst, new object[] { path, obj });
        
        var setDelegateMethod = webViewType.GetMethod("SetDelegateObject");
        setDelegate = (inst, obj) => setDelegateMethod.Invoke(inst, new object[] { obj });
        
        var setViewMethod = webViewType.GetMethod("SetHostView");
        setView = (inst, view) => setViewMethod.Invoke(inst, new object[] { view });
        
        var setRectMethod = webViewType.GetMethod("SetSizeAndPosition");
        setRect = (inst, rect) => setRectMethod.Invoke(inst, new object[] { (int) rect.x, (int) rect.y, (int) rect.width, (int)rect.height });
        
        var setFocusMethod = webViewType.GetMethod("SetFocus");
        setFocus = (inst, focus) => setFocusMethod.Invoke(inst, new object[] { focus });
        
        var hasAppFocusMethod = webViewType.GetMethod("HasApplicationFocus");
        hasAppFocus = (inst) => (bool)hasAppFocusMethod.Invoke(inst, new object[0]);
        
        var setAppFocusMethod = webViewType.GetMethod("SetApplicationFocus");
        setAppFocus = (inst, focus) => setAppFocusMethod.Invoke(inst, new object[] { focus });
        
        var showMethod = webViewType.GetMethod("Show");
        show = (inst) => showMethod.Invoke(inst, new object[0]);
        
        var hideMethod = webViewType.GetMethod("Hide");
        hide = (inst) => hideMethod.Invoke(inst, new object[0]);
        
        var backMethod = webViewType.GetMethod("Back");
        back = (inst) => backMethod.Invoke(inst, new object[0]);
        
        var forwardMethod = webViewType.GetMethod("Forward");
        forward = (inst) => forwardMethod.Invoke(inst, new object[0]);
        
        var sendMethod = webViewType.GetMethod("SendOnEvent");
        send = (inst, ev) => sendMethod.Invoke(inst, new object[] { ev });
        
        var reloadMethod = webViewType.GetMethod("Reload");
        reload = (inst) => reloadMethod.Invoke(inst, new object[0]);

        var allowMethod = webViewType.GetMethod("AllowRightClickMenu");
        allow = (inst, al) => allowMethod.Invoke(inst, new object[] { al });

        var devMethod = webViewType.GetMethod("ShowDevTools");
        dev = (inst) => devMethod.Invoke(inst, new object[0]);

        var toggleMethod = webViewType.GetMethod("ToggleMaximaze");
        toggle = (inst) => toggleMethod.Invoke(inst, new object[0]);

    }

    public delegate void InitDelegate(object instance, object view, int x, int y, int width, int height, bool handles);

}


public sealed class WebView {
    public ScriptableObject web;

    public WebView() {
        Debug.Log("WebView: constructor");
        this.web = WebProvider.create();
        this.web.hideFlags = HideFlags.HideAndDontSave;
    }

    ~WebView() {
        Debug.Log("WebView: destructor");
    }

    public void init(object view, Rect rect, bool handles) {
        Debug.Log("WebView: init: this: " + this + " web: " + ((web == null) ? "NULL" : ("" + web)) + " view: " + ((view == null) ? "NULL" : ("" + view)) + " rect: " + rect + " handles: " + handles);
        WebProvider.init(this.web, view, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, handles);
        Debug.Log("WebView: init: done");
    }

    public void executeJS(string code) {
        WebProvider.execute(this.web, code);
    }

    public void loadURL(string url) {
        WebProvider.loadURL(this.web, url);
    }

    public void loadFile(string path) {
        WebProvider.loadURL(this.web, path);
    }

    public void defineScript(string path, ScriptableObject obj) {
        WebProvider.define(this.web, path, obj);
    }

    public void setDelegateObject(ScriptableObject obj) {
        WebProvider.setDelegate(this.web, obj);
    }

    public void setHostView(object view) {
        WebProvider.setView(this.web, view);
    }

    public void setRect(Rect rect) {
        WebProvider.setRect(this.web, rect);
    }

    public void setFocus(bool focus) {
        WebProvider.setFocus(this.web, focus);
    }

    public bool hasApplicationFocus() {
        return WebProvider.hasAppFocus(this.web);
    }

    public void setApplicationFocus(bool focus) {
        WebProvider.setAppFocus(this.web, focus);
    }

    public void show() {
        WebProvider.show(this.web);
    }

    public void hide() {
        WebProvider.hide(this.web);
    }

    public void back() {
        WebProvider.back(this.web);
    }

    public void forward() {
        WebProvider.forward(this.web);
    }

    public void sendOnEvent(string json) {
        WebProvider.send(this.web, json);
    }

    public void reload() {
        WebProvider.reload(this.web);
    }

    public void allowRightClickMenu(bool allow) {
        WebProvider.allow(this.web, allow);
    }

    public void showDevTools() {
        WebProvider.dev(this.web);
    }

    public void toggleMaximaze() {
        WebProvider.toggle(this.web);
    }

    public void destroy() {
        UnityEngine.Object.DestroyImmediate(this.web);
    }
}


public class CallbackWrapper {
    public object callback;

    public CallbackWrapper(object callback) {
        this.callback = callback;
    }

    public void Send(string message) {
        WebProvider.callback(this.callback, message);
    }
}


#endif
