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


////////////////////////////////////////////////////////////////////////
// UnityJS.mm for OSX and iOS


////////////////////////////////////////////////////////////////////////
// Includes


#if TARGET_OS_OSX

// OSX

#if false
// Old but buggy.
#define UNITYJS_WEBVIEW 1
#define UNITYJS_WKWEBVIEW 0
#else
// New but no image.
#define UNITYJS_WEBVIEW 0
#define UNITYJS_WKWEBVIEW 1
#endif

#import <Carbon/Carbon.h>
#import <AppKit/AppKit.h>
#import <WebKit/WebKit.h>
#import <OpenGL/gl.h>
#import <Metal/Metal.h>

#if UNITY_JS_WKWEBVIEW

#import <WebKit/WKWebsiteDataStore.h>
#import <WebKit/WKSnapshotConfiguration.h>

#endif

#else

// iOS

#define UNITYJS_WEBVIEW 0
#define UNITYJS_WKWEBVIEW 1

#import <OpenGLES/ES2/gl.h>
#import <UIKit/UIKit.h>
#import <WebKit/WebKit.h>

#endif

#include "IUnityGraphics.h"
#include "IUnityGraphicsMetal.h"


////////////////////////////////////////////////////////////////////////
// Declarations


@class CUnityJSPlugin;


typedef void (*UnityRenderEventFunc)(int eventId);


extern "C" {

    void _CUnityJSPlugin_SetUnitySendMessageCallback(void *unitySendMessageCallback);
    void *_CUnityJSPlugin_Init(BOOL transparent);
    void _CUnityJSPlugin_Destroy(void *instance);
    void _CUnityJSPlugin_SetRect(void *instance, int width, int height);
    void _CUnityJSPlugin_SetVisibility(void *instance, BOOL visibility);
    void _CUnityJSPlugin_LoadURL(void *instance, const char *url);
    void _CUnityJSPlugin_EvaluateJS(void *instance, const char *js);
    void _CUnityJSPlugin_EvaluateJSReturnResult(void *instance, const char *js);
    BOOL _CUnityJSPlugin_CanGoBack(void *instance);
    BOOL _CUnityJSPlugin_CanGoForward(void *instance);
    void _CUnityJSPlugin_GoBack(void *instance);
    void _CUnityJSPlugin_GoForward(void *instance);
    const char *_CUnityJSPlugin_GetPluginID(void *instance);
    void _CUnityJSPlugin_RenderIntoTextureSetup(void *instance, int width, int height);
    long _CUnityJSPlugin_GetRenderTextureHandle(void *instance);
    int _CUnityJSPlugin_GetRenderTextureWidth(void *instance);
    int _CUnityJSPlugin_GetRenderTextureHeight(void *instance);
    UnityRenderEventFunc _CUnityJSPlugin_GetRenderEventFunc();
    void _CUnityJSPlugin_UnityRenderEvent(int eventId);
    void _CUnityJSPlugin_FlushCaches(void *instance);

#if TARGET_OS_OSX

    void _CUnityJSPlugin_Update(void *instance, int x, int y, float deltaY, BOOL buttonDown, BOOL buttonPress, BOOL buttonRelease, BOOL keyPress, unsigned char keyCode, const char *keyChars);

#else

    UIViewController *UnityGetGLViewController();
    void UnitySendMessage(const char *, const char *, const char *);

#endif

}


////////////////////////////////////////////////////////////////////////
// CUnityJSPlugin


@interface CUnityJSPlugin :

#if UNITYJS_WEBVIEW
    NSObject
#endif
#if UNITYJS_WKWEBVIEW
    NSObject<WKUIDelegate, WKNavigationDelegate, WKScriptMessageHandler>
#endif

{

#if UNITYJS_WEBVIEW
    WebView *webView;
#endif

#if UNITYJS_WKWEBVIEW
    WKWebView *webView;
#endif

    NSString *pluginID;
    bool renderIntoTexture;
    GLuint renderTextureHandle;
    int renderTextureWidth;
    int renderTextureHeight;
    BOOL confirmPanelResult;
    NSString *textInputPanelResult;

#if TARGET_OS_OSX
    NSBitmapImageRep *renderTextureBitmap;
#endif

}

@end


////////////////////////////////////////////////////////////////////////
// Globals


static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);
static IUnityInterfaces *unityInterfaces;
static IUnityGraphics *unityGraphics;
static IUnityGraphicsMetal *unityGraphicsMetal;
static UnityGfxRenderer unityRendererType = kUnityGfxRendererNull;


typedef void (*UnitySendMessageCallback)(const char *target, const char *method, const char *message);
static UnitySendMessageCallback unitySendMessageCallback;

static int currentPluginID = 0;
static NSMutableDictionary *plugins = nil;


////////////////////////////////////////////////////////////////////////
// Unity Extension Interface
// These are called by Unity when the plugin loads and unloads.


// Called by Unity when the plugin loads.
// Caches interface pointers, hooks up the other Unity callbacks and initializes.
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(
    IUnityInterfaces *unityInterfaces0)
{
    unityInterfaces = unityInterfaces0;
    //NSLog(@"CUnityJSPlugin: UnityPluginLoad: unityInterfaces: %ld", (long)unityInterfaces);
    unityGraphics = unityInterfaces->Get<IUnityGraphics>();
    //NSLog(@"CUnityJSPlugin: UnityPluginLoad: unityGraphics: %ld", (long)unityGraphics);
    unityGraphicsMetal = unityInterfaces->Get<IUnityGraphicsMetal>();
    //NSLog(@"CUnityJSPlugin: UnityPluginLoad: unityGraphicsMetal: %ld", (long)unityGraphicsMetal);
    unityGraphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
    OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}


// Called by Unity when the plugin unloads.
// Unregisters Unity callbacks.
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
    //NSLog(@"CUnityJSPlugin: UnityPluginUnload");
    unityGraphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
}


// Called by Unity to track graphics device state.
// Caches and clears the renderer type.
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(
    UnityGfxDeviceEventType eventType)
{
    //NSLog(@"CUnityJSPlugin: OnGraphicsDeviceEvent: eventType: %d", (int)eventType);

    switch (eventType) {
        case kUnityGfxDeviceEventInitialize: {
            currentPluginID = 0;
            unityRendererType = unityGraphics->GetRenderer();
            NSLog(@"CUnityJSPlugin: OnGraphicsDeviceEvent: kUnityGfxDeviceEventInitialize: unityRendererType: %d", (int)unityRendererType);
            // TODO: user initialization code
            break;
        }
        case kUnityGfxDeviceEventShutdown: {
            unityRendererType = kUnityGfxRendererNull;
            NSLog(@"CUnityJSPlugin: OnGraphicsDeviceEvent: kUnityGfxDeviceEventShutdown: unityRendererType: %d", (int)unityRendererType);
            for (NSString *key in [plugins allKeys]) {
                [plugins removeObjectForKey:key];
            }
            break;
        }
        case kUnityGfxDeviceEventBeforeReset: {
            // TODO: user Direct3D 9 code
            break;
        }
        case kUnityGfxDeviceEventAfterReset: {
            // TODO: user Direct3D 9 code
            break;
        }
    };
}


@implementation CUnityJSPlugin


- (id)init:(BOOL)transparent
{
    self = [super init];

    pluginID =
        [NSString stringWithFormat:@"%i", ++currentPluginID]; // never 0

    if (plugins == nil) {
        plugins = [[NSMutableDictionary alloc] init];
    }

    @synchronized(plugins) {
        plugins[pluginID] = self;
    }

#if UNITYJS_WEBVIEW

#if TARGET_OS_OSX

    // OSX WebView

    webView =
        [[WebView alloc]
            initWithFrame:
                NSMakeRect(0, 0, 256, 256)];

    webView.hidden = YES;
    [webView setDrawsBackground:!transparent];
    [webView setFrameLoadDelegate:(id)self];
    [webView setPolicyDelegate:(id)self];

    //NSLog(@"CUnityJSPlugin: init: Created WebView: %@", webView);

#else

    // iOS WebView: Depricated

#endif

#endif

#if UNITYJS_WKWEBVIEW

    // OSX and iOS WkWebView

    WKWebViewConfiguration *configuration =
        [[WKWebViewConfiguration alloc] init];

    WKUserContentController *userContentController =
        [[WKUserContentController alloc] init];

    [userContentController
        addScriptMessageHandler:self
                           name:@"bridge"];

    [userContentController
        addScriptMessageHandler:self
                           name:@"log"];

    NSString *source =
        @"window.console.logOld = window.console.log; window.console.log = function(arg) { window.webkit.messageHandlers.log.postMessage(Array.prototype.slice.call(arguments).join(' ')); window.console.logOld.apply(this, arguments); };";
    WKUserScript *userScript =
        [[WKUserScript alloc]
             initWithSource:source
             injectionTime:WKUserScriptInjectionTimeAtDocumentStart
             forMainFrameOnly:YES];
    [userContentController addUserScript:userScript];

    configuration.userContentController = userContentController;

#if TARGET_OS_OSX
    NSRect frame = NSMakeRect(0, 0, 256, 256);
#else
    CGRect frame = CGRectMake(0, 0, 256, 256);
#endif

    webView =
        [[WKWebView alloc] 
            initWithFrame:frame
            configuration:configuration];

    webView.UIDelegate = self;
    webView.navigationDelegate = self;

#if TARGET_OS_OSX

    webView.hidden = NO;

    [webView.configuration.preferences
        setValue:@TRUE
          forKey:@"allowFileAccessFromFileURLs"];

#else

    webView.hidden = YES;

    if (transparent) {
        webView.opaque = NO;
        webView.backgroundColor = [UIColor clearColor];
    }

    UIView *view = UnityGetGLViewController().view;
    [view addSubview:webView];

#endif

#endif

    return self;
}


- (void)dealloc
{
    NSLog(@"CUnityJSPlugin: dealloc pluginID %@ webView %@", pluginID, webView);

    if (plugins != nil) {
        @synchronized(plugins) {
            [plugins removeObjectForKey:pluginID];
        }
    }

    [webView removeFromSuperview];
    webView = nil;
}


- (NSString *)URLDecode:(NSString *)stringToDecode
{
    NSString *result = [stringToDecode stringByReplacingOccurrencesOfString:@"+" withString:@" "];
    result = [result stringByRemovingPercentEncoding];
    return result;
}


- (void)setVisibility:(BOOL)visibility
{
    if (webView == nil) {
        return;
    }

    //webView.hidden = visibility ? NO : YES;
}


- (void)setRect:(int)width height:(int)height
{
    if (webView == nil) {
        return;
    }

    //NSLog(@"CUnityJSPlugin: setRect: pluginID: %@, width: %d height: %d", pluginID, width, height);

    webView.frame =
#if TARGET_OS_OSX
        NSMakeRect(0, 0, width, height);
#else
        CGRectMake(0, 0, width, height);
#endif
}


- (void)loadURL:(const char *)url
{
    NSLog(@"CUnityJSPlugin: loadURL: pluginID: %@ url: %s", pluginID, url);

    if (webView == nil) {
        return;
    }

    NSString *urlStr = [NSString stringWithUTF8String:url];
    NSURL *nsurl = [NSURL URLWithString:urlStr];

#if UNITYJS_WEBVIEW
    NSURLRequest *request = [NSURLRequest requestWithURL:nsurl];
    [[webView mainFrame] loadRequest:request];
#endif

#if UNITYJS_WKWEBVIEW
    if ([urlStr hasPrefix:@"file:"]) {
        NSURL *top = [NSURL URLWithString:[urlStr stringByDeletingLastPathComponent]];
        [webView loadFileURL:nsurl allowingReadAccessToURL:top];
    } else {
        NSURLRequest *request = [NSURLRequest requestWithURL:nsurl];
        [webView loadRequest:request];
    }
#endif

}


- (void)evaluateJS:(const char *)js
{
    //NSLog(@"CUnityJSPlugin: evaluateJS: pluginID: %@ js: %s", pluginID, js);

    if (webView == nil) {
        return;
    }

    NSString *jsStr = [NSString stringWithUTF8String:js];

#if UNITYJS_WEBVIEW
    [webView stringByEvaluatingJavaScriptFromString:jsStr];
#endif

#if UNITYJS_WKWEBVIEW
    [webView evaluateJavaScript:jsStr completionHandler:^(NSString *result, NSError *error) {
        //NSLog(@"CUnityJSPlugin: evaluateJS: completion: result: %@ error: %@", result, error);
    }];
#endif

}


- (void)evaluateJSReturnResult:(const char *)js
{
    //NSLog(@"CUnityJSPlugin: evaluateJSReturnResult: pluginID: %@ js: %s", pluginID, js);

    if (webView == nil) {
        return;
    }

    NSString *jsStr = [NSString stringWithUTF8String:js];

#if UNITYJS_WEBVIEW
    NSString *result = [webView stringByEvaluatingJavaScriptFromString:jsStr];
    [self
        unitySendMessage:@"ResultFromJS"
                    data:result];
#endif

#if UNITYJS_WKWEBVIEW
    [webView evaluateJavaScript:jsStr completionHandler:^(NSString *result, NSError *error) {
        //NSLog(@"CUnityJSPlugin: evaluateJSReturnResult: completion: result: %@ error: %@", result, error);
        [self
            unitySendMessage:@"ResultFromJS"
                        data:result];
    }];
#endif

}


- (void)unitySendMessage:(NSString *)method
    data:(NSString *)data
{
    if (unitySendMessageCallback == nil) {
        //NSLog(@"CUnityJSPlugin: unitySendMessage: called without unitySendMessageCallback");
        return;
    }

    @autoreleasepool {

        unitySendMessageCallback(
            [pluginID UTF8String],
            [method UTF8String],
            [data UTF8String]);

    }

}


- (BOOL)canGoBack
{
    if (webView == nil) {
        return false;
    }

    return [webView canGoBack];
}


- (BOOL)canGoForward
{
    if (webView == nil) {
        return false;
    }

    return [webView canGoForward];
}


- (void)goBack
{
    if (webView == nil) {
        return;
    }

    [webView goBack];
}


- (void)goForward
{
    if (webView == nil) {
        return;
    }

    [webView goForward];
}


- (NSString *)getPluginID
{
    return pluginID;
}


- (void)renderIntoTextureSetup:(int)width height:(int)height
{

#if TARGET_OS_OSX

#if false

    bool resetBitmap =
        (bitmap == nil) ||
        (renderTextureHandle == 0) ||
        (renderTextureWidth != width) ||
        (renderTextureHeight != height);

    renderIntoTexture = true;
    renderTextureWidth = width;
    renderTextureHeight = height;

    if (resetBitmap) {
        bitmap = nil;
        [self setRect:renderTextureWidth height:renderTextureHeight];
    }

    NSRect renderTextureRect =
        NSMakeRect(0, 0, renderTextureWidth, renderTextureHeight);

    if (bitmap == nil) {
        bitmap =
            [webView
                bitmapImageRepForCachingDisplayInRect:renderTextureRect];
    }

    memset([bitmap bitmapData], 0, [bitmap bytesPerRow] * [bitmap pixelsHigh]);

    [webView
        cacheDisplayInRect:renderTextureRect
        toBitmapImageRep:bitmap];

#else

    renderTextureWidth = width;
    renderTextureHeight = height;
    renderTextureBitmap = nil;

    if (![[webView class] respondsToSelector:@selector(takeSnapshotWithConfiguration:completionHandler:)]) {
        //NSLog(@"CUnityJSPlugin: renderIntoTextureSetup: webView %@ does not respond to selector takeSnapshotWithConfiguration:completionHandler", webView);
    } else {

        [webView takeSnapshotWithConfiguration:nil
            completionHandler:^(NSImage *snapshotImage, NSError *error) {
                if (error) {
                    NSLog(@"CUnityJSPlugin: renderIntoTextureSetup: takeSnapshotWithConfiguration: ERROR: %@ renderTextureWidth: %d renderTextureHeight: %d unityRendererType: %d", error, self->renderTextureWidth, self->renderTextureHeight, (int)unityRendererType);
                } else {
                    NSSize imageSize = [snapshotImage size];

                    [snapshotImage lockFocus];
                    self->renderTextureBitmap =
                        [[NSBitmapImageRep alloc]
                            initWithFocusedViewRect:
                                NSMakeRect(0.0, 0.0, imageSize.width, imageSize.height)];
                    [snapshotImage unlockFocus];

                    self->renderIntoTexture = true;
                }
             }];
    }

#endif

    //NSLog(@"CUnityJSPlugin: renderIntoTextureSetup: renderTextureWidth: %d renderTextureHeight: %d unityRendererType: %d", renderTextureWidth, renderTextureHeight, (int)unityRendererType);

#endif

}


- (long)getRenderTextureHandle
{
    return (long)renderTextureHandle;
}


- (int)getRenderTextureWidth
{
    return renderTextureWidth;
}


- (int)getRenderTextureHeight
{
    return renderTextureHeight;
}


+ (void)renderUpdateWebViewPlugins
{
    //NSLog(@"CUnityJSPlugin: renderUpdateWebViewPlugins");

    if ((plugins == nil) ||
        ([plugins count] == 0)) {
        return;
    }

    @synchronized(plugins) {
    
        for (NSString *key in plugins) {
            CUnityJSPlugin *plugin = (CUnityJSPlugin *)plugins[key];
            [plugin renderUpdate];
        }

    }
}


- (void)renderUpdate
{
    //NSLog(@"CUnityJSPlugin: renderUpdate: pluginID: %@ renderIntoTexture: %d unityRendererType: %d", pluginID, renderIntoTexture, unityRendererType);
    
    if (!renderIntoTexture) {
        return;
    }
    
    //NSLog(@"CUnityJSPlugin: renderUpdate: pluginID: %@ renderIntoTexture unityRendererType: %d", pluginID, unityRendererType);

    renderIntoTexture = false;

    switch (unityRendererType) {
        case kUnityGfxRendererOpenGLCore:
            [self renderUpdateOpenGLCore];
            break;
        case kUnityGfxRendererOpenGLES20:
            [self renderUpdateOpenGLES20];
            break;
        case kUnityGfxRendererOpenGLES30:
            [self renderUpdateOpenGLES30];
            break;
        case kUnityGfxRendererMetal:
            [self renderUpdateMetal];
            break;
        default:
            NSLog(@"CUnityJSPlugin: renderUpdate: pluginID: %@ unknown unityRendererType: %d", pluginID, unityRendererType);
            break;
    }

}


- (void)renderUpdateOpenGLCore
{

#if TARGET_OS_OSX
    NSLog(@"CUnityJSPlugin: renderUpdateOpenGLCore: pluginID: %@", pluginID);

    if (renderTextureHandle == 0) {

        glGenTextures(1, &renderTextureHandle);
        glBindTexture(GL_TEXTURE_2D, renderTextureHandle);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, renderTextureWidth, renderTextureHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, 0);
        glBindTexture(GL_TEXTURE_2D, renderTextureHandle);

        //NSLog(@"CUnityJSPlugin: renderUpdateOpenGLCore: pluginID: %@ created renderTextureHandle: %d width: %d height: %d", pluginID, renderTextureHandle, renderTextureWidth, renderTextureHeight);
    }

    int samplesPerPixel = (int)[renderTextureBitmap samplesPerPixel];
    int rowLength = 0;
    int unpackAlign = 0;

    glGetIntegerv(GL_UNPACK_ROW_LENGTH, &rowLength);
    glGetIntegerv(GL_UNPACK_ALIGNMENT, &unpackAlign);
    glPixelStorei(GL_UNPACK_ROW_LENGTH, (GLint)[renderTextureBitmap bytesPerRow] / samplesPerPixel);
    glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
    glBindTexture(GL_TEXTURE_2D, renderTextureHandle);

    if (![renderTextureBitmap isPlanar] &&
        ((samplesPerPixel == 3) ||
         (samplesPerPixel == 4))) {

        NSLog(@"CUnityJSPlugin: renderUpdateOpenGLCore: pluginID: %@ initializing image samplesPerPixel: %d w: %d h: %d", pluginID, samplesPerPixel, (int)[renderTextureBitmap pixelsWide], (int)[renderTextureBitmap pixelsHigh]);

        glTexSubImage2D(
            GL_TEXTURE_2D,
            0,
            0,
            0,
            (GLsizei)renderTextureWidth,
            (GLsizei)renderTextureHeight,
            samplesPerPixel == 4 ? GL_RGBA : GL_RGB,
            GL_UNSIGNED_BYTE,
            [renderTextureBitmap bitmapData]);

        NSLog(@"CUnityJSPlugin: renderUpdateOpenGLCore: pluginID: %@ initialized image", pluginID);

    }

    glBindTexture(GL_TEXTURE_2D, 0);

    glPixelStorei(GL_UNPACK_ROW_LENGTH, rowLength);
    glPixelStorei(GL_UNPACK_ALIGNMENT, unpackAlign);

    NSLog(@"CUnityJSPlugin: renderUpdateOpenGLCore: pluginID: %@ sending Texture", pluginID);
    
    renderTextureBitmap = nil;

    [self
        unitySendMessage:@"Texture"
        data:@""];

    NSLog(@"CUnityJSPlugin: renderUpdateOpenGLCore: pluginID: %@ sent Texture", pluginID);
#endif

}


- (void)renderUpdateOpenGLES20
{
    NSLog(@"CUnityJSPlugin: renderUpdateOpenGLES20: pluginID: %@", pluginID);
}


- (void)renderUpdateOpenGLES30
{
    NSLog(@"CUnityJSPlugin: renderUpdateOpenGLES30: pluginID: %@", pluginID);
}


- (void)renderUpdateMetal
{
    NSLog(@"CUnityJSPlugin: renderUpdateMetal: pluginID: %@", pluginID);

#if TARGET_OS_OSX

    if (renderTextureHandle == 0) {

        glGenTextures(1, &renderTextureHandle);
        glBindTexture(GL_TEXTURE_2D, renderTextureHandle);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, renderTextureWidth, renderTextureHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, 0);
        glBindTexture(GL_TEXTURE_2D, renderTextureHandle);

        //NSLog(@"CUnityJSPlugin: renderUpdateMetal: pluginID: %@ created renderTextureHandle: %d width: %d height: %d", pluginID, renderTextureHandle, renderTextureWidth, renderTextureHeight);
    }

    int samplesPerPixel = (int)[renderTextureBitmap samplesPerPixel];
    int rowLength = 0;
    int unpackAlign = 0;

    glGetIntegerv(GL_UNPACK_ROW_LENGTH, &rowLength);
    glGetIntegerv(GL_UNPACK_ALIGNMENT, &unpackAlign);
    glPixelStorei(GL_UNPACK_ROW_LENGTH, (GLint)[renderTextureBitmap bytesPerRow] / samplesPerPixel);
    glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
    glBindTexture(GL_TEXTURE_2D, renderTextureHandle);

    if (![renderTextureBitmap isPlanar] &&
        ((samplesPerPixel == 3) ||
         (samplesPerPixel == 4))) {

        NSLog(@"CUnityJSPlugin: renderUpdateMetal: pluginID: %@ initializing image samplesPerPixel: %d w: %d h: %d", pluginID, samplesPerPixel, (int)[renderTextureBitmap pixelsWide], (int)[renderTextureBitmap pixelsHigh]);

        glTexSubImage2D(
            GL_TEXTURE_2D,
            0,
            0,
            0,
            (GLsizei)renderTextureWidth,
            (GLsizei)renderTextureHeight,
            samplesPerPixel == 4 ? GL_RGBA : GL_RGB,
            GL_UNSIGNED_BYTE,
            [renderTextureBitmap bitmapData]);

        NSLog(@"CUnityJSPlugin: renderUpdateMetal: pluginID: %@ initialized image", pluginID);

    }

    glBindTexture(GL_TEXTURE_2D, 0);

    glPixelStorei(GL_UNPACK_ROW_LENGTH, rowLength);
    glPixelStorei(GL_UNPACK_ALIGNMENT, unpackAlign);

    NSLog(@"CUnityJSPlugin: renderUpdateMetal: pluginID: %@ sending CallOnTexture", pluginID);
    
    renderTextureBitmap = nil;

    [self
        unitySendMessage:@"Texture"
        data:@""];

    NSLog(@"CUnityJSPlugin: renderUpdateMetal: pluginID: %@ sent CallOnTexture", pluginID);

#endif

}


#if UNITYJS_WEBVIEW


- (void)flushCaches
{
    // TODO: OSX WebView flushCaches
}


// WebFrameLoadDelegate
// https://developer.apple.com/documentation/webkit/webframeloaddelegate?language=objc


// Called if an error occurs when starting to load data for a page.
// The frame continues to display the committed data source if there is one.
- (void)webView:(WebView *)sender
    didFailProvisionalLoadWithError:(NSError *)error
    forFrame:(WebFrame *)frame
{
    [self
        unitySendMessage:@"Error"
                    data:[error description]];
}


// Called when an error occurs loading a committed data source.
// This method is called after the data source has been committed
// but resulted in an error.
- (void)webView:(WebView *)sender
    didFailLoadWithError:(NSError *)error
    forFrame:(WebFrame *)frame
{
    [self
        unitySendMessage:@"Error"
                    data:[error description]];
}


// Called when a page load completes.
// This method is invoked when a location request for frame has
// completed; that is, when all the resources are done loading.
// Additional information about the request can be obtained from the
// data source of frame.
- (void)webView:(WebView *)sender
    didFinishLoadForFrame:(WebFrame *)frame
{
    [self
        unitySendMessage:@"Loaded"
                    data:[[[[frame dataSource] request] URL] absoluteString]];
}


// WebPolicyDelegate
// https://developer.apple.com/documentation/webkit/webpolicydelegate?language=objc


- (void)webView:(WebView *)sender
    decidePolicyForNavigationAction:(NSDictionary *)actionInformation
    request:(NSURLRequest *)request
    frame:(WebFrame *)frame
    decisionListener:(id<WebPolicyDecisionListener>)listener
{
    NSString *url = [[request URL] absoluteString];
    //NSLog(@"CUnityJSPlugin: decidePolicyForNavigationAction: url: %@", url);

    if ([url hasPrefix:@"unity:"]) {
        NSString *message = [url substringFromIndex:6];
        //NSLog(@"CUnityJSPlugin: decidePolicyForNavigationAction: hasPrefix unity: message: %@", message);
        [self
            unitySendMessage:@"CallFromJS"
                        data:[self URLDecode: message]];
        [listener ignore];
        return;
    }

    NSString *fragment = [[request URL] fragment];

    if ([fragment hasPrefix:@"unity:"]) {
        NSString *message = [fragment substringFromIndex:6];
        //NSLog(@"CUnityJSPlugin: decidePolicyForNavigationAction: hasPrefix unity: message: %@", message);
        [self
            unitySendMessage:@"CallFromJS"
                        data:[self URLDecode:message]];
        [listener ignore];
        return;
    }

    [listener use];
}


#endif


#if UNITYJS_WKWEBVIEW


- (void)flushCaches
{
    NSSet *websiteDataTypes =
        [WKWebsiteDataStore allWebsiteDataTypes];
    NSDate *dateFrom =
        [NSDate dateWithTimeIntervalSince1970:0];
    [[WKWebsiteDataStore defaultDataStore]
         removeDataOfTypes:websiteDataTypes
         modifiedSince:dateFrom
         completionHandler:^{
             // Done
         }];
}


// WkUIDelegate
// https://developer.apple.com/documentation/webkit/wkuidelegate?language=objc


- (void)webView:(WKWebView *)webView
    runJavaScriptAlertPanelWithMessage:(NSString *)message
    initiatedByFrame:(WKFrameInfo *)frame
    completionHandler:(void (^)(void))completionHandler
{
    completionHandler();
}


- (void)webView:(WKWebView *)webView
    runJavaScriptConfirmPanelWithMessage:(NSString *)message
    initiatedByFrame:(WKFrameInfo *)frame
    completionHandler:(void (^)(BOOL result))completionHandler
{
    completionHandler(confirmPanelResult);
}


- (void)webView:(WKWebView *)webView
    runJavaScriptTextInputPanelWithPrompt:(NSString *)prompt
    defaultText:(NSString *)defaultText
    initiatedByFrame:(WKFrameInfo *)frame
    completionHandler:(void (^)(NSString *result))completionHandler
{
    completionHandler(
        (textInputPanelResult == nil)
            ? defaultText
            : textInputPanelResult);
}


- (void)webView:(WKWebView *)webView
    runOpenPanelWithParameters:(WKOpenPanelParameters *)parameters
    initiatedByFrame:(WKFrameInfo *)frame
    completionHandler:(void (^)(NSArray<NSURL *> *URLs))completionHandler
{
    completionHandler(nil);
}


// WKNavigationDelegate
// https://developer.apple.com/documentation/webkit/wknavigationdelegate?language=objc


- (void)webView:(WKWebView *)webView
    didFailProvisionalNavigation:(WKNavigation *)navigation
    withError:(NSError *)error
{
    [self
        unitySendMessage:@"Error"
                    data:[error description]];
}

- (void)webView:(WKWebView *)webView
    didFailNavigation:(WKNavigation *)navigation
    withError:(NSError *)error
{
    [self
        unitySendMessage:@"Error"
                    data:[error description]];
}


- (void)webView:(WKWebView *)wkWebView
    didFinishNavigation:(WKNavigation *)navigation
{
    if (wkWebView == nil) {
        return;
    }

    [wkWebView
        evaluateJavaScript:@"document.readyState"
         completionHandler:^(NSString *result, NSError *error) {
            if (result != nil &&
                error == nil &&
                [result isEqualToString:@"complete"]) {

                [self
                    unitySendMessage:@"Loaded"
                    data:[[wkWebView URL] absoluteString]];
            }

         }];
}


- (void)webView:(WKWebView *)wkWebView
    decidePolicyForNavigationAction:(WKNavigationAction *)navigationAction
    decisionHandler:(void (^)(WKNavigationActionPolicy))decisionHandler
{
    NSString *url = [[navigationAction.request URL] absoluteString];
    //NSLog(@"CUnityJSPlugin: decidePolicyForNavigationAction: url: %@", url);

    if (wkWebView == nil) {
        NSLog(@"CUnityJSPlugin: decidePolicyForNavigationAction: wkWebView is nil");

        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    }

#if !TARGET_OS_OSX

    if ([url rangeOfString:@"//itunes.apple.com/"].location != NSNotFound) {
        //NSLog(@"CUnityJSPlugin: decidePolicyForNavigationAction: iTunes");

        [[UIApplication sharedApplication]
            openURL:[navigationAction.request URL]];

        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    }

#endif

    if ([url hasPrefix:@"unity:"]) {
        NSString *message = [url substringFromIndex:6];
        //NSLog(@"CUnityJSPlugin: decidePolicyForNavigationAction: hasPrefix unity: message: %@", message);

        [self
            unitySendMessage:@"CallFromJS"
            data:[self URLDecode:message]];

        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    }

    NSString *fragment = [[navigationAction.request URL] fragment];

    if ([fragment hasPrefix:@"unity:"]) {
        NSString *message = [fragment substringFromIndex:6];
        //NSLog(@"CUnityJSPlugin: decidePolicyForNavigationAction: hasPrefix unity: message: %@", message);

        [self
            unitySendMessage:@"CallFromJS"
            data:[self URLDecode:message]];

        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    }

    if (navigationAction.navigationType == WKNavigationTypeLinkActivated &&
       (!navigationAction.targetFrame || !navigationAction.targetFrame.isMainFrame)) {
        //NSLog(@"CUnityJSPlugin: decidePolicyForNavigationAction: _blank link");
        // cf. for target="_blank", cf. http://qiita.com/ShingoFukuyama/items/b3a1441025a36ab7659c

        [webView
            loadRequest:navigationAction.request];

        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    }

    //NSLog(@"CUnityJSPlugin: decidePolicyForNavigationAction: allow");
    decisionHandler(WKNavigationActionPolicyAllow);
}


// WKScriptMessageHandler
// https://developer.apple.com/documentation/webkit/wkscriptmessagehandler?language=objc


- (void)userContentController:(WKUserContentController *)userContentController
      didReceiveScriptMessage:(WKScriptMessage *)message
{
    // Log the message received
    NSLog(@"CUnityJSPlugin: userContentController didReceiveScriptMessage: Received event message body %@ frameInfo %@ name %@ webView %@", message.body, message.frameInfo, message.name, message.webView);

    if ([message.name isEqualToString:@"log"]) {
        [self
            unitySendMessage:@"ConsoleMessage"
                        data:message.body];
    } else if ([message.name isEqualToString:@"bridge"]) {
        [self
            unitySendMessage:@"MessageFromJS"
                        data:message.body];
    } else {
    }

}


#endif


@end


////////////////////////////////////////////////////////////////////////
// External Interface


void _CUnityJSPlugin_SetUnitySendMessageCallback(void *unitySendMessageCallback_)
{
    //NSLog(@"CUnityJSPlugin: _CUnityJSPlugin_SetUnitySendMessageCallback: unitySendMessageCallback: %ld", (long)unitySendMessageCallback_);
    unitySendMessageCallback = (UnitySendMessageCallback)unitySendMessageCallback_;
}


void *_CUnityJSPlugin_Init(
    BOOL transparent)
{
    id instance =
        [[CUnityJSPlugin alloc]
            init:transparent];

    return (__bridge_retained void *)instance;
}


void _CUnityJSPlugin_Destroy(void *instance)
{
    CUnityJSPlugin *webViewPlugin = (__bridge_transfer CUnityJSPlugin *)instance;

    NSString *pluginID = [webViewPlugin getPluginID];
    NSLog(@"CUnityJSPlugin: _CUnityJSPlugin_Destroy: webViewPlugin: %@ pluginID %@", webViewPlugin, pluginID);
    [plugins removeObjectForKey:pluginID];

    webViewPlugin = nil;
}


void _CUnityJSPlugin_SetRect(void *instance, int width, int height)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    [webViewPlugin setRect:width height:height];
}


void _CUnityJSPlugin_SetVisibility(void *instance, BOOL visibility)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    [webViewPlugin setVisibility:visibility];
}


void _CUnityJSPlugin_LoadURL(void *instance, const char *url)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    [webViewPlugin loadURL:url];
}


void _CUnityJSPlugin_EvaluateJS(void *instance, const char *js)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    [webViewPlugin evaluateJS:js];
}


void _CUnityJSPlugin_EvaluateJSReturnResult(void *instance, const char *js)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    [webViewPlugin evaluateJSReturnResult:js];
}


BOOL _CUnityJSPlugin_CanGoBack(void *instance)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    return [webViewPlugin canGoBack];
}


BOOL _CUnityJSPlugin_CanGoForward(void *instance)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    return [webViewPlugin canGoForward];
}


void _CUnityJSPlugin_GoBack(void *instance)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    [webViewPlugin goBack];
}


void _CUnityJSPlugin_GoForward(void *instance)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    [webViewPlugin goForward];
}


const char *_CUnityJSPlugin_GetPluginID(void *instance)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    const char *s = [[webViewPlugin getPluginID] UTF8String];
    char *r = (char *)malloc(strlen(s) + 1);
    strcpy(r, s);
    return r;
}


void _CUnityJSPlugin_RenderIntoTextureSetup(void *instance, int width, int height)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    [webViewPlugin renderIntoTextureSetup:width height:height];
}


long _CUnityJSPlugin_GetRenderTextureHandle(void *instance)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    return [webViewPlugin getRenderTextureHandle];
}


int _CUnityJSPlugin_GetRenderTextureWidth(void *instance)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    return [webViewPlugin getRenderTextureWidth];
}


int _CUnityJSPlugin_GetRenderTextureHeight(void *instance)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    return [webViewPlugin getRenderTextureHeight];
}


UnityRenderEventFunc _CUnityJSPlugin_GetRenderEventFunc()
{
    return _CUnityJSPlugin_UnityRenderEvent;
}


void _CUnityJSPlugin_UnityRenderEvent(int eventId)
{
    @autoreleasepool {

        switch (eventId) {

            case 0: {
                //NSLog(@"CUnityJSPlugin: _CUnityJSPlugin_UnityRenderEvent: StartUp: eventId: %d", (int)eventId);
                break;
            }

            case 1: {
                //NSLog(@"CUnityJSPlugin: _CUnityJSPlugin_UnityRenderEvent: ShutDown: eventId: %d", (int)eventId);
                break;
            }

            case 2: {
                //NSLog(@"CUnityJSPlugin: _CUnityJSPlugin_UnityRenderEvent: RenderUpdateWebViewPlugins: eventId: %d", (int)eventId);

                [CUnityJSPlugin renderUpdateWebViewPlugins];

                break;
            }

        }

    }
}


void _CUnityJSPlugin_FlushCaches(void *instance)
{
    CUnityJSPlugin *webViewPlugin = (__bridge CUnityJSPlugin *)instance;
    [webViewPlugin flushCaches];
}


////////////////////////////////////////////////////////////////////////
