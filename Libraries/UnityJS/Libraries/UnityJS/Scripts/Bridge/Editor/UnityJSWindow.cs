#if UNITY_EDITOR && USE_CEF


////////////////////////////////////////////////////////////////////////
// From:
// https://raw.githubusercontent.com/BenjaminMoore/UnityWebViewInEditor/master/Assets/Editor/WebWindow.cs


using UnityEngine;
using UnityEditor;


public class UnityJSWindow : EditorWindow {

    
    public static Rect windowRect = new Rect(100, 100, 512, 512);


    public delegate void StartedEventHandler();
    public delegate void SendEventHandler(string events);

    public event StartedEventHandler startedEvent;
    public event SendEventHandler sendEvent;
    public WebView webView;


    public UnityJSWindow()
    {
        Debug.Log("UnityJSWindow: constructor!");
    }


    ~UnityJSWindow()
    {
        Debug.Log("UnityJSWindow: destructor!");
    }


#if false
    [MenuItem("Window/Unit-JS WebView")]
    public static void Open()
    {
        Debug.Log("UnityJSWindow: Open");

        bool utility = false; // Create a normal window, not a utility window.
        string title = "Unity-JS WebView";
        bool focus = false;

#if false
        Debug.Log("UnityJSWindow: Open: calling GetWindow<UnityJSWindow>");
        UnityJSWindow unityJSWindow = EditorWindow.GetWindow<UnityJSWindow>(utility, title, focus);
#endif

#if true
        Debug.Log("UnityJSWindow: Open: calling CreateInstance<UnityJSWindow>");
        UnityJSWindow unityJSWindow = ScriptableObject.CreateInstance<UnityJSWindow>();
        unityJSWindow.titleContent = new GUIContent(title);
#endif

        Debug.Log("UnityJSWindow: Open: calling unityJSWindow: " + unityJSWindow + " Show");
        unityJSWindow.Show();

        Debug.Log("UnityJSWindow: Open: calling unityJSWindow: " + unityJSWindow + " Init bridge: " + Bridge.bridge);
        unityJSWindow.Init();
    }
#endif


    public void Init()
    {
        Debug.Log("UnityJSWindow: Init: webView: " + webView);

        InitWebView();
    }


    public void InitWebView()
    {
        if (webView != null) {
            return;
        }

        position = windowRect;

        Debug.Log("UnityJSWindow: Init: making new WebView");
        webView = new WebView();

        object p = WebProvider.getView(this);
        Debug.Log("UnityJSWindow: Init: WebView init: this: " + this + " p: " + p);
        webView.init(p, new Rect(0, 0, position.width, position.height), false);

        Debug.Log("UnityJSWindow: Init: WebView defineScript");
        webView.defineScript("Bridge", this);
        webView.allowRightClickMenu(true);
        webView.setDelegateObject(this);
        //Debug.Log("UnityJSWindow: Init: WebView showDevTools");
        //webView.showDevTools();

        string url;
        if (Bridge.bridge == null) {

            url = "about:blank";

        } else {

#if true
            url =
                "file://" +
                Application.dataPath +
                "/WebGLTemplates/Bridge" +
                "/bridge.html";
#else
                "http://localhost/Bridge/bridge.html";
                //"http://DonHopkins.com/home/Bridge/bridge.html";
#endif

            url += "?random=" + Random.value;

            Debug.Log("UnityJSWindow: Init: url: " + url);
        }

        Debug.Log("UnityJSWindow: Init: LoadURL: " + url);
        LoadURL(url);
    }


    public void SendEventList(string evListString, object callback)
    {
        Debug.Log("UnityJSWindow: SendEventList: callback: " + callback + " evListString: " + evListString);

        if (Bridge.bridge == null) {
            Debug.Log("UnityJSWindow: SendEventList: no bridge. evListString: " + evListString);
            return;
        }

        if (sendEvent != null) {
            Debug.Log("UnityJSWindow: SendEventList: firing sendEvent: evListString: " + evListString);
            sendEvent(evListString);
            Debug.Log("UnityJSWindow: SendEventList: fired sendEvent");
        }
    }


    public void OnLoadError(string url)
    {
        Debug.Log("UnityJSWindow: OnLoadError: url: " + url);
    }


    public void OnBatchMode()
    {
        Debug.Log("UnityJSWindow: OnBatchMode");
    }


    public void OnLocationChanged(string url)
    {
        Debug.Log("UnityJSWindow: OnLocationChanged: url: " + url + " startedEvent: " + startedEvent);

        if (startedEvent != null) {
            Debug.Log("UnityJSWindow: OnLocationChanged: firing startedEvent");
            startedEvent();
            Debug.Log("UnityJSWindow: OnLocationChanged: fired startedEvent");
        }
    }


    public void OnInitScripting()
    {
        Debug.Log("UnityJSWindow: OnInitScripting: bridge: " + Bridge.bridge);
    }


    public void OnOpenExternalLink(string url)
    {
        Debug.Log("UnityJSWindow: OnOpenExternalLink: url: " + url);

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            return;

        Application.OpenURL(url);
    }


    public void OnEnable()
    {
        Debug.Log("UnityJSWindow: OnEnable");
        //InitWebView();
    }


    public void OnDisable()
    {
        Debug.Log("UnityJSWindow: OnDisable");
    }


    public void OnDownloadProgress(string id, string message, ulong bytes, ulong total)
    {
        Debug.Log("UnityJSWindow: OnDownloadProgress: id: " + id + " message: " + message + " bytes: " + bytes + " total: " + total);
    }


    public void OnFocus()
    {
        Debug.Log("UnityJSWindow: OnFocus");
    }

    public void OnLostFocus()
    {
        Debug.Log("UnityJSWindow: OnLostFocus");
    }


    public void OnBecameInvisible()
    {
        Debug.Log("UnityJSWindow: OnBecameInvisible");
    }


    public void OnGUI()
    {
        if (Event.current.type == EventType.Layout) {
            //InitWebView();
        }
    }


    public void OnDestroy()
    {
        Debug.Log("UnityJSWindow: OnDestroy: webView: " + webView);

        if (webView != null) {
            Debug.Log("UnityJSWindow: OnDestroy: destroying webView: " + webView);
            webView.destroy();
            webView = null;
            Debug.Log("UnityJSWindow: OnDestroy: destroyed webView");
        }

    }


    public void LoadURL(string url)
    {
        if (webView == null) {
            Debug.LogError("UnityJSWindow: LoadURL: webView not defined!");
            return;
        }

        webView.loadURL(url);
    }
    

    public void EvaluateJS(string js)
    {
        if (webView == null) {
            Debug.LogError("UnityJSWindow: EvaluateJS: webView not defined!");
            return;
        }

        webView.executeJS(js);
    }
    

    public void Reload()
    {
        if (webView == null) {
            Debug.LogError("UnityJSWindow: Reload: webView not defined!");
            return;
        }

        webView.reload();
    }
    

}


#endif
