/*
 * Copyright (C) 2011 Keijiro Takahashi
 * Copyright (C) 2012 GREE, Inc.
 * Copyright (C) 2017 by Don Hopkins, Ground Up Software.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */


package com.groundupsoftware.unityjs;


import java.util.HashMap;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.graphics.Canvas;
import android.graphics.Bitmap;
import android.graphics.Point;
import android.net.Uri;
import android.os.Build;
import android.util.Log;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup.LayoutParams;
import android.widget.FrameLayout;
import android.webkit.JavascriptInterface;
import android.webkit.WebChromeClient;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.webkit.ValueCallback;
import android.opengl.GLES20;

import com.unity3d.player.UnityPlayer;


class CUnityJSPluginInterface {

    private static String TAG = "CUnityJSPluginInterface";

    private CUnityJSPlugin plugin;
    private String pluginID;


    public CUnityJSPluginInterface(CUnityJSPlugin plugin_)
    {
        plugin = plugin_;
    }


    @JavascriptInterface
    public void call(final String message)
    {
        call("CallFromJS", message);
    }


    @JavascriptInterface
    public void returnResult(final String message)
    {
        //Log.d(TAG, "ReturnResult: message: " + message);
        call("ReturnResultFromJS", message);
    }


    public void call(final String method, final String message)
    {
        final Activity a = UnityPlayer.currentActivity;
        a.runOnUiThread(new Runnable() {public void run() {
            if (plugin.IsInitialized()) {
                CUnityJSPlugin.UnitySendMessage(plugin.pluginID, method, message);
            }
        }});
    }


}


class CustomWebView extends WebView {


    private static String TAG = "CustomWebView";

    public ExternalSurface externalSurface;
    public boolean disableExternalSurfaceDraw;
    public boolean disableSuperDraw;


    public CustomWebView(Context context)
    {
        super(context);
        //Log.d(TAG, "constructor: context: " + context);
    }


    @Override
    protected void onDraw(Canvas canvas)
    {
        //Log.d(TAG, "onDraw: externalSurface: " + externalSurface + " disableExternalSurfaceDraw: " + disableExternalSurfaceDraw + " disableSuperDraw: " + disableSuperDraw);

        if ((externalSurface != null) &&
            !disableExternalSurfaceDraw) {
            try {
                final Canvas surfaceCanvas = externalSurface.surface.lockCanvas(null);
                //Log.d(TAG, "onDraw: surfaceCanvas: " + surfaceCanvas);
                super.onDraw(surfaceCanvas);
                //Log.d(TAG, "onDraw: posting");
                externalSurface.surface.unlockCanvasAndPost(surfaceCanvas);
                //Log.d(TAG, "onDraw: posted");
            } catch (Exception ex) {
                Log.e(TAG, "onDraw: Error: Exception drawing WebView! ex: " + ex);
                ex.printStackTrace();
            }
        }

        //Log.d(TAG, "onDraw: normal super onDraw. canvas: " + canvas);
        if (!disableSuperDraw) {
            super.onDraw(canvas);
            return;
        }
    }


}


public class CUnityJSPlugin {


    private static String TAG = "CUnityJSPlugin";
    private static FrameLayout layout = null; // TODO: Do not put Android context classes in static fields.
    private static int currentPluginID = 0;
    private static HashMap<String, CUnityJSPlugin> plugins = new HashMap<String, CUnityJSPlugin>();
    private static final Object pluginLock = new Object();

    public String pluginID;
    private CustomWebView webView;
    private CUnityJSPluginInterface unityJSPlugin;
    private boolean canGoBack;
    private boolean canGoForward;
    private long renderTextureHandle;
    private int renderTextureWidth;
    private int renderTextureHeight;
    private ExternalSurface externalSurface;
    private boolean renderIntoTexture;


    static {
        System.loadLibrary("UnityJS");
    }


    // Java_com_groundupsoftware_unityjs_CUnityJSPlugin_SetUnitySendMessageCallback
    public native static void SetUnitySendMessageCallback(long sendMessageCallback);

    // Java_com_groundupsoftware_unityjs_CUnityJSPlugin_SetUnitySendMessageCallback
    public native static long UnitySendMessage(String target, String method, String message);

    // Java_com_groundupsoftware_unityjs_CUnityJSPlugin_GetRenderEventFunc
    public native static long GetRenderEventFunc();


    public CUnityJSPlugin()
    {
        synchronized (pluginLock) {

            pluginID = "" + ++currentPluginID; // Never zero.
            plugins.put(pluginID, this);

        }
    }


    public boolean IsInitialized()
    {
        return webView != null;
    }


    public void Init(final boolean transparent)
    {
        //Log.d(TAG, "Init: this: " + this + " transparent: " + transparent);

        final CUnityJSPlugin self = this;
        final Activity a = UnityPlayer.currentActivity;

        a.runOnUiThread(new Runnable() {public void run() {

            //Log.d(TAG, "Init: runOnUiThread: webView: " + webView);

            if (webView != null) {
                return;
            }

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT) {
                WebView.setWebContentsDebuggingEnabled(true);
            }

            // To draw the entire area.
            // https://developer.android.com/reference/android/webkit/WebView.html#enableSlowWholeDocumentDraw()
            //WebView.enableSlowWholeDocumentDraw();

            //Log.d(TAG, "Init: runOnUiThread: creating new CustomWebView");
            webView = new CustomWebView(a);
            //Log.d(TAG, "Init: runOnUiThread: created new CustomWebView: " + webView);

            //Log.d(TAG, "Init: runOnUiThread: creating new ExternalSurface");
            externalSurface = new ExternalSurface();
            //Log.d(TAG, "Init: runOnUiThread: created new ExternalSurface: " + externalSurface);

            webView.externalSurface = externalSurface;
            webView.disableExternalSurfaceDraw = true;
            webView.disableSuperDraw = false;

            //webView.setVisibility(View.GONE);
            //webView.setFocusable(true);
            //webView.setFocusableInTouchMode(true);
            webView.setVisibility(View.VISIBLE);
            webView.setFocusable(false);
            webView.setFocusableInTouchMode(false);

            webView.setLayerType(View.LAYER_TYPE_SOFTWARE, null);
            //webView.setLayerType(View.LAYER_TYPE_HARDWARE, null);
            webView.setInitialScale(100);

            webView.setWebChromeClient(new WebChromeClient() {

                @Override
                public boolean onConsoleMessage(android.webkit.ConsoleMessage cm) {
                    //Log.d(TAG, "onConsoleMessage: " + cm.message());
                    unityJSPlugin.call("CallOnConsoleMessage", cm.message());
                    return true;
                }

            });

            unityJSPlugin =
                new CUnityJSPluginInterface(self);
            webView.addJavascriptInterface(unityJSPlugin , "Unity");

            webView.setWebViewClient(new WebViewClient() {

                @Override
                public void onReceivedError(WebView view, int errorCode, String description, String failingUrl) {
                    webView.loadUrl("about:blank");
                    canGoBack = webView.canGoBack();
                    canGoForward = webView.canGoForward();
                    unityJSPlugin.call("CallOnError", errorCode + "\t" + description + "\t" + failingUrl);
                }

                @Override
                public void onPageStarted(WebView view, String url, Bitmap favicon) {
                    canGoBack = webView.canGoBack();
                    canGoForward = webView.canGoForward();
                }

                @Override
                public void onPageFinished(WebView view, String url) {
                    canGoBack = webView.canGoBack();
                    canGoForward = webView.canGoForward();
                    unityJSPlugin.call("CallOnLoaded", url);
                }

                @Override
                public boolean shouldOverrideUrlLoading(WebView view, String url) {
                    if (url.startsWith("http://") || 
                        url.startsWith("https://") ||
                        url.startsWith("file://") || 
                        url.startsWith("javascript:")) {
                        // Let webview handle the URL
                        return false;
                    } else if (url.startsWith("unity:")) {
                        String message = url.substring(6);
                        unityJSPlugin.call("CallFromJS", message);
                        return true;
                    }
                    Intent intent = new Intent(Intent.ACTION_VIEW, Uri.parse(url));
                    view.getContext().startActivity(intent);
                    return true;
                }

            });

            WebSettings webSettings = webView.getSettings();
            webSettings.setSupportZoom(false);
            webSettings.setJavaScriptEnabled(true);
            webSettings.setDatabaseEnabled(true);
            webSettings.setDomStorageEnabled(true);
            String databasePath = webView.getContext().getDir("databases", Context.MODE_PRIVATE).getPath();
            webSettings.setDatabasePath(databasePath);
            webSettings.setAllowUniversalAccessFromFileURLs(true);
            int SDK_INT = android.os.Build.VERSION.SDK_INT;
            if (SDK_INT > 16) {
                webSettings.setMediaPlaybackRequiresUserGesture(false);
            }

            if (transparent) {
                webView.setBackgroundColor(0x00000000);
            }

            if (layout == null) {

                layout = new FrameLayout(a);

                a.addContentView(
                    layout,
                    new LayoutParams(
                        LayoutParams.MATCH_PARENT,
                        LayoutParams.MATCH_PARENT));

                layout.setZ(-10.0f);

                //layout.setFocusable(true);
                //layout.setFocusableInTouchMode(true);
                layout.setFocusable(false);
                layout.setFocusableInTouchMode(false);

            }

            layout.addView(
                webView,
                new FrameLayout.LayoutParams(
                    LayoutParams.MATCH_PARENT,
                    LayoutParams.MATCH_PARENT,
                    Gravity.NO_GRAVITY));

        }});

        final View activityRootView = a.getWindow().getDecorView().getRootView();

        activityRootView.getViewTreeObserver().addOnGlobalLayoutListener(new android.view.ViewTreeObserver.OnGlobalLayoutListener() {
            @Override
            public void onGlobalLayout() {
                android.graphics.Rect r = new android.graphics.Rect();
                // r will be populated with the coordinates of your view that area still visible.
                activityRootView.getWindowVisibleDisplayFrame(r);
                android.view.Display display = a.getWindowManager().getDefaultDisplay();
                Point size = new Point();
                display.getSize(size);
                int heightDiff = activityRootView.getRootView().getHeight() - (r.bottom - r.top);
                //System.out.print(String.format("[NativeWebview] %d, %d\n", size.y, heightDiff));
                if (heightDiff > size.y / 3) { // assume that this means that the keyboard is on
                    CUnityJSPlugin.UnitySendMessage(pluginID, "SetKeyboardVisible", "true");
                } else {
                    CUnityJSPlugin.UnitySendMessage(pluginID, "SetKeyboardVisible", "false");
                }
            }
        });
    }


    public void Destroy()
    {
        //Log.d(TAG, "Destroy: mWebView: " + webView);

        // TODO: clean up webView's ExternalSurface.

        synchronized (pluginLock) {

            if ((pluginID != null) &&
                (plugins != null) &&
                plugins.containsKey(pluginID)) {
                plugins.remove(pluginID);
            }

        }

        final Activity a = UnityPlayer.currentActivity;
        a.runOnUiThread(new Runnable() {public void run() {

            if (webView == null) {
                return;
            }

            layout.removeView(webView);
            webView = null;

        }});
    }


    public void LoadURL(final String url)
    {
        //Log.d(TAG, "LoadURL: " + url + " this: " + this);
        final Activity a = UnityPlayer.currentActivity;
        a.runOnUiThread(new Runnable() {public void run() {
            if (webView == null) {
                return;
            }
            webView.loadUrl(url);
        }});
    }


    public void EvaluateJS(final String js)
    {
        //Log.d(TAG, "EvaluateJS: " + js + " this: " + this);
        final Activity a = UnityPlayer.currentActivity;
        a.runOnUiThread(new Runnable() {public void run() {
            if (webView == null) {
                return;
            }
            webView.evaluateJavascript(js, null);
        }});
    }


    public void EvaluateJSReturnResult(final String js)
    {
        //Log.d(TAG, "EvaluateJSReturnResult: " + js + " this: " + this);
        final Activity a = UnityPlayer.currentActivity;
        a.runOnUiThread(new Runnable() {public void run() {
            if (webView == null) {
                return;
            }
            webView.evaluateJavascript(js, new ValueCallback<String>() {
                @Override
                public void onReceiveValue(String result) {
                    //Log.d(TAG, "EvaluateJS: onReceiveValue: " + result);
                    unityJSPlugin.returnResult(result);
                }
            });
        }});
    }


    public void GoBack()
    {
        final Activity a = UnityPlayer.currentActivity;
        a.runOnUiThread(new Runnable() {public void run() {
            if (webView == null) {
                return;
            }
            webView.goBack();
        }});
    }


    public void GoForward()
    {
        final Activity a = UnityPlayer.currentActivity;
        a.runOnUiThread(new Runnable() {public void run() {
            if (webView == null) {
                return;
            }
            webView.goForward();
        }});
    }


    public void SetRect(int width, int height)
    {
        final FrameLayout.LayoutParams params =
            new FrameLayout.LayoutParams(
                width,
                height,
                Gravity.NO_GRAVITY);
        //params.setMargins(0, 0, 0, 0);
        final Activity a = UnityPlayer.currentActivity;
        a.runOnUiThread(new Runnable() {public void run() {
            if (webView == null) {
                return;
            }
            webView.setLayoutParams(params);
        }});
    }


    public void SetVisibility(final boolean visibility)
    {
        final Activity a = UnityPlayer.currentActivity;
        a.runOnUiThread(new Runnable() {public void run() {
            if (webView == null) {
                return;
            }
            if (visibility) {
                webView.setVisibility(View.VISIBLE);
                //layout.requestFocus();
                //webView.requestFocus();
            } else {
                webView.setVisibility(View.GONE);
            }
        }});
    }


    public void RenderIntoTextureSetup(final int width, final int height)
    {
        //Log.d(TAG, "RenderIntoTextureSetup: width: " + width + " height: " + height + " this: " + this);
        renderIntoTexture = true;
        renderTextureWidth = width;
        renderTextureHeight = height;
    }


    public String GetPluginID()
    {
        return pluginID;
    }


    public long GetRenderTextureHandle()
    {
        return renderTextureHandle;
    }
    

    public int GetRenderTextureWidth()
    {
        return renderTextureWidth;
    }
    

    public int GetRenderTextureHeight()
    {
        return renderTextureHeight;
    }
    

    public static void RenderUpdateUnityJSPlugins()
    {
        //Log.d(TAG, "RenderUpdateUnityJSPlugins");
        synchronized (pluginLock) {

            for (CUnityJSPlugin plugin : plugins.values()) {
                plugin.RenderUpdate();
            }

        }
    }


    public void RenderUpdate()
    {
        //Log.d(TAG, "RenderUpdate: renderTextureHandle: " + renderTextureHandle + " renderTextureWidth: " + renderTextureWidth + " renderTextureHeight: " + renderTextureHeight + " this: " + this);

        RenderIntoTexture();
        UpdateExternalSurface();
    }


    public void RenderIntoTexture()
    {
        if (!renderIntoTexture) {
            return;
        }

        renderIntoTexture = false;

        if (externalSurface == null) {
            Log.e(TAG, "RenderIntoTexture: missing externalSurface");
            return;
        }

        //Log.d(TAG, "RenderIntoTexture: calling setupOutputTexture: renderTextureWidth: " + renderTextureWidth + " renderTextureHeight: " + renderTextureHeight);
        renderTextureHandle = (long)externalSurface.setupOutputTexture(renderTextureWidth, renderTextureHeight);
        //Log.d(TAG, "RenderIntoTexture: called setupOutputTexture: renderTextureHandle: " + renderTextureHandle);
        if (checkGlError("RenderIntoTexture: After setupOutputTexture")) {
            // Caught below.
            //Log.d(TAG, "RenderIntoTexture: Error after setupOutputTexture");
            //return;
        }

        if (renderTextureHandle == 0) {
            //Log.d(TAG, "RenderIntoTexture: null renderTextureHandle, not doing anything");
            return;
        }

        //Log.d(TAG, "RenderIntoTexture: calling runOnUiThread");
        final Activity a = UnityPlayer.currentActivity;
        a.runOnUiThread(new Runnable() {public void run() {
            //Log.d(TAG, "RenderIntoTexture: runOnUiThread: starting webview draw on ui thread");

            if (unityJSPlugin == null) {
                return;
            }

            //Log.d(TAG, "RenderIntoTexture: runOnUiThread: lockCanvas: surface: " + externalSurface.surface + " this: " + this);
            Canvas canvas = externalSurface.surface.lockCanvas(null);
            //Log.d(TAG, "RenderIntoTexture: runOnUiThread: got canvas: " + canvas + ", now drawing" + " this: " + this);

            try {
                //Log.d(TAG, "RenderIntoTexture: runOnUiThread: drawing webView: " + webView + " pluginID: " + pluginID + " canvas: " + canvas + " this: " + this);
                webView.draw(canvas);
            } catch (Exception ex) {
                //Log.e(TAG, "RenderIntoTexture: runOnUiThread: error during draw: " + ex + " this: " + this);
                ex.printStackTrace();
            }
            //Log.d(TAG, "RenderIntoTexture: runOnUiThread: drew, now unlocking canvas: " + canvas);

            externalSurface.surface.unlockCanvasAndPost(canvas);
            //Log.d(TAG, "RenderIntoTexture: runOnUiThread: posted, now updating this: " + this);

            //Log.d(TAG, "RenderIntoTexture: runOnUiThread: ending webview draw on ui thread");
        }});
        //Log.d(TAG, "RenderIntoTexture: called runOnUiThread");

        //Log.d(TAG, "RenderIntoTexture: finished");
    }


    public void UpdateExternalSurface()
    {
        if (externalSurface == null) {
            Log.e(TAG, "UpdateExternalSurface: externalSurface is null");
            return;
        }

        boolean updated = externalSurface.update();
        //Log.d(TAG, "UpdateExternalSurface: updated: " + updated);

        if (checkGlError("UpdateExternalSurface: After update")) {
            Log.e(TAG, "UpdateExternalSurface: Error after update");
            return;
        }

        if (!updated) {
            return;
        }

        //Log.d(TAG, "UpdateExternalSurface: surface updated so calling CallOnTexture");
        unityJSPlugin.call("CallOnTexture", "");
    }


    private static boolean checkGlError(String op)
    {
        boolean gotError = false;
        int error;
        while ((error = GLES20.glGetError()) != GLES20.GL_NO_ERROR) {
            if (!gotError) {
                Log.e(TAG, "checkGlError: !!!!!!!!!!!!!!!! ERROR: " + op);
            }
            gotError = true;
            Log.e(TAG, "checkGlError: error: " + error);
        }

        return gotError;
    }


}
