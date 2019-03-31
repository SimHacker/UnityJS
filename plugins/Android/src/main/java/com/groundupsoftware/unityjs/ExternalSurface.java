////////////////////////////////////////////////////////////////////////
// Copyright (C) 2017 by Don Hopkins, Ground Up Software.


package com.groundupsoftware.unityjs;


import java.nio.ByteOrder;
import java.nio.ByteBuffer;
import java.nio.IntBuffer;
import java.nio.FloatBuffer;
import java.util.Random;
import android.graphics.SurfaceTexture;
import android.util.Log;
import android.view.Surface;
import android.opengl.GLES20;
import android.opengl.GLES11Ext;
import android.opengl.GLUtils;
import android.opengl.Matrix;


class ExternalSurface
    implements SurfaceTexture.OnFrameAvailableListener {

    private static String TAG = "ExternalSurface";

    public SurfaceTexture surfaceTexture;
    public Surface surface;
    private int externalTextureHandle;
    private int outputTextureHandle;
    private int framebufferHandle;
    private int framebufferWidth;
    private int framebufferHeight;
    private int vertexBufferHandle;
    private int programHandle;
    private int uMatrixHandle;
    private int aPositionHandle;
    private int aTextureHandle;
    private float[] uMatrix = new float[16];
    private long lastTimestamp;
    private boolean initialized;
    private boolean broken;
    private boolean frameAvailable;
    private Random random = new Random();

    private static final int FLOAT_SIZE_BYTES = 4;
    private static final int VERTICES_DATA_STRIDE_BYTES =      5 * FLOAT_SIZE_BYTES;
    private static final int VERTICES_DATA_POS_OFFSET_BYTES =  0 * FLOAT_SIZE_BYTES;
    private static final int VERTICES_DATA_UV_OFFSET_BYTES =   3 * FLOAT_SIZE_BYTES;

    private final float[] verticesData = {
        // X, Y, Z, U, V
        -1f, -1f, 0f, 0f, 0f,
         1f, -1f, 0f, 1f, 0f,
        -1f,  1f, 0f, 0f, 1f,
         1f,  1f, 0f, 1f, 1f,
    };

    private FloatBuffer vertices;

    private static final String VERTEX_SHADER =
        "uniform mat4 uMatrix;\n" +
        "attribute vec4 aPosition;\n" +
        "attribute vec4 aTexture;\n" +
        "varying vec2 vTexture;\n" +
        "void main() {\n" +
        "  gl_Position = aPosition;\n" +
        "  vTexture = (uMatrix * aTexture).xy;\n" +
        "}\n";

    private static final String FRAGMENT_SHADER =
        "#extension GL_OES_EGL_image_external : require\n" +
        //"#extension GL_OES_EGL_image_external_essl3 : enable\n" +
        "precision mediump float;\n" +
        "varying vec2 vTexture;\n" +
        "uniform samplerExternalOES sTexture;\n" +
        "void main() {\n" +
        "  gl_FragColor = texture2D(sTexture, vTexture);\n" +
        "}\n";


    ////////////////////////////////////////////////////////////////////////


    public ExternalSurface()
    {
    }


    public void initialize()
    {
        GLES20.glDisable(GLES20.GL_DEPTH_TEST);
        GLES20.glDisable(GLES20.GL_SCISSOR_TEST);
        GLES20.glDisable(GLES20.GL_STENCIL_TEST);
        GLES20.glDisable(GLES20.GL_CULL_FACE);
        GLES20.glDisable(GLES20.GL_BLEND);
        GLES20.glDisable(GLES20.GL_POLYGON_OFFSET_FILL);
        GLES20.glPolygonOffset(GLES20.GL_ELEMENT_ARRAY_BUFFER, 0);
        GLES20.glDepthMask(false);
        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, 0);
        GLES20.glBindBuffer(GLES20.GL_ELEMENT_ARRAY_BUFFER, 0);

        if (initialized) {
            return;
        }

        //Log.d(TAG, "initialize: initializing");

        initialized = true;
        broken = false;

        synchronized(this) {
            frameAvailable = false;
        }

        //Log.d(TAG, "initialize: generating texture externalTextureHandle");
        int[] textures = new int[1];
        GLES20.glGenTextures(1, textures, 0);
        if (checkGlError("glGenTextures externalTextureHandle")) {
            //Log.d(TAG, "initialize: glGenTextures externalTextureHandle failed");
            broken = true;
            return;
        }
        externalTextureHandle = textures[0];
        //Log.d(TAG, "initialize: generated texture externalTextureHandle: " + externalTextureHandle);

        Log.d(TAG, "initialize: saving last texture lastExternalTextureHandles");
        int[] lastExternalTextureHandles = new int[1];
        GLES20.glGetIntegerv(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, lastExternalTextureHandles, 0);
        if (checkGlError("glBindTexture glGetIntegerv GL_TEXTURE_EXTERNAL_OES")) {
            Log.d(TAG, "initialize: glGetIntegerv GL_TEXTURE_EXTERNAL_OES failed");
            //broken = true;
            //return;
        }
        Log.d(TAG, "initialize: saved last texture lastExternalTextureHandles " + lastExternalTextureHandles[0]);

        Log.d(TAG, "initialize: binding texture externalTextureHandle " + externalTextureHandle);
        GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, externalTextureHandle);
        if (checkGlError("glBindTexture externalTextureHandle")) {
            //Log.d(TAG, "initialize: glBindTexture failed");
            broken = true;
            return;
        }
        Log.d(TAG, "initialize: bound texture externalTextureHandle " + externalTextureHandle);

        Log.d(TAG, "initialize: calling glTexParameterf");
        GLES20.glTexParameterf(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_MIN_FILTER, GLES20.GL_NEAREST);
        GLES20.glTexParameterf(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_MAG_FILTER, GLES20.GL_LINEAR);
        GLES20.glTexParameteri(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_WRAP_S, GLES20.GL_CLAMP_TO_EDGE);
        GLES20.glTexParameteri(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_WRAP_T, GLES20.GL_CLAMP_TO_EDGE);
        if (checkGlError("glTexParameteri")) {
            //Log.d(TAG, "initialize: glTexParameteri failed");
            broken = true;
            return;
        }
        Log.d(TAG, "initialize: called glTexParameterf");

        Log.d(TAG, "initialize: restoring binding texture lastExternalTextureHandles " + lastExternalTextureHandles[0]);
        GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, lastExternalTextureHandles[0]);
        if (checkGlError("glBindTexture lastExternalTextureHandles")) {
            //Log.d(TAG, "initialize: glBindTexture lastExternalTextureHandles failed");
            //broken = true;
            //return;
        }
        Log.d(TAG, "initialize: restoring bound texture lastExternalTextureHandles " + lastExternalTextureHandles[0]);

        //Log.d(TAG, "initialize: creating new SurfaceTexture with externalTextureHandle: " + externalTextureHandle);
        surfaceTexture = new SurfaceTexture(externalTextureHandle);
        surfaceTexture.setOnFrameAvailableListener(this);
        //Log.d(TAG, "initialize: surfaceTexture: " + surfaceTexture);

        //Log.d(TAG, "initialize: creating new Surface with surfaceTexture: " + surfaceTexture);
        surface = new Surface(surfaceTexture);
        //Log.d(TAG, "initialize: created surface: " + surface);

        //Log.d(TAG, "initialize: creating vertex buffer");
        int[] vbhandles = new int[1];
        //Log.d(TAG, "initialize: glGenBuffers");
        GLES20.glGenBuffers(1, vbhandles, 0);
        vertexBufferHandle = vbhandles[0];
        if (checkGlError("glGenBuffers")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "initialize: glBindBuffer GL_ARRAY_BUFFER vertexBufferHandle: " + vertexBufferHandle);
        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, vertexBufferHandle);
        if (checkGlError("glBindBuffer vertexBufferHandle: " + vertexBufferHandle)) {
            broken = true;
            return;
        }

        int bufferSize = verticesData.length * FLOAT_SIZE_BYTES;
        //Log.d(TAG, "initialize: creating vertices verticesData.length: " + verticesData.length + " bufferSize: " + bufferSize);
        vertices = ByteBuffer.allocateDirect(bufferSize)
            .order(ByteOrder.nativeOrder()).asFloatBuffer();
        vertices.put(verticesData).position(0);

        //Log.d(TAG, "initialize: glBufferData bufferSize: " + bufferSize);
        GLES20.glBufferData(GLES20.GL_ARRAY_BUFFER, bufferSize, vertices, GLES20.GL_STATIC_DRAW);
        if (checkGlError("glBufferData")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "initialize: glBindBuffer GL_ARRAY_BUFFER 0");
        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, 0);
        if (checkGlError("glBindBuffer 0")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "initialize: creating program");
        programHandle = createProgram(VERTEX_SHADER, FRAGMENT_SHADER);
        //Log.d(TAG, "initialize: created program programHandle: " + programHandle);
        if (programHandle == 0) {
            //Log.d(TAG, "initialize: error creating program");
            broken = true;
            return;
        }

        //Log.d(TAG, "initialize: glGetUniformLocation uMatrix");
        uMatrixHandle = GLES20.glGetUniformLocation(programHandle, "uMatrix");
        if (checkGlError("glGetUniformLocation uMatrix") ||
            (uMatrixHandle == -1)) {
            //Log.d(TAG, "initialize: error in glGetUniformLocation uMatrix");
            broken = true;
            return;
        }
        //Log.d(TAG, "initialize: uMatrixHandle: " + uMatrixHandle);

        //Log.d(TAG, "initialize: glGetAttribLocation aPosition");
        aPositionHandle = GLES20.glGetAttribLocation(programHandle, "aPosition");
        if (checkGlError("glGetAttribLocation aPosition") ||
            (aPositionHandle == -1)) {
            broken = true;
            return;
        }
        //Log.d(TAG, "initialize: aPositionHandle: " + aPositionHandle);

        //Log.d(TAG, "initialize: glGetAttribLocation aTexture");
        aTextureHandle = GLES20.glGetAttribLocation(programHandle, "aTexture");
        //Log.d(TAG, "initialize: externalTextureHandle: " + aTextureHandle);
        if (checkGlError("glGetAttribLocation aTexture") ||
            (aTextureHandle == -1)) {
            //Log.d(TAG, "initialize: error in glGetAttribLocation aTexture");
            broken = true;
            return;
        }
        //Log.d(TAG, "initialize: still aTextureHandle: " + aTextureHandle);

    }


    public int setupOutputTexture(int width, int height)
    {
        //Log.d(TAG, "setupOutputTexture: calling initialize. initialized: " + initialized + " broken: " + broken);
        initialize();
        //Log.d(TAG, "setupOutputTexture: called initialize. initialized: " + initialized + " broken: " + broken);

        if (broken) {
            return 0;
        }

        boolean resize =
            (framebufferHandle == 0) ||
            (outputTextureHandle == 0) ||
            (framebufferWidth != width) ||
            (framebufferWidth != height);

        if (!resize) {
            return outputTextureHandle;
        }

        //Log.d(TAG, "setupOutputTexture: resizing to width: " + width + " height: " + height);
        framebufferWidth = width;
        framebufferHeight = height;

        if (outputTextureHandle == 0) {

            //Log.d(TAG, "setupOutputTexture: generating texture handle outputTextureHandle");
            int[] handles = {0};
            GLES20.glGenTextures(1, handles, 0);
            if (checkGlError("glGenTextures")) {
                broken = true;
                return 0;
            }
            outputTextureHandle = handles[0];
            //Log.d(TAG, "setupOutputTexture: generated outputTextureHandle: " + outputTextureHandle);

            //Log.d(TAG, "setupOutputTexture: binding texture outputTextureHandle: " + outputTextureHandle);
            GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, outputTextureHandle);
            if (checkGlError("glBindTexture")) {
                broken = true;
                return 0;
            }
            //Log.d(TAG, "setupOutputTexture: bound texture outputTextureHandle: " + outputTextureHandle);

            //Log.d(TAG, "setupOutputTexture: setting texture parameters");
            GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_MIN_FILTER, GLES20.GL_LINEAR);
            GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_MAG_FILTER, GLES20.GL_LINEAR);
            GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_WRAP_S, GLES20.GL_CLAMP_TO_EDGE);
            GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_WRAP_T, GLES20.GL_CLAMP_TO_EDGE);
            if (checkGlError("glTexParameteri")) {
                broken = true;
                return 0;
            }

            //Log.d(TAG, "setupOutputTexture: glPixelStorei");
            GLES20.glPixelStorei(GLES20.GL_UNPACK_ALIGNMENT, 1);
            if (checkGlError("glPixelStorei")) {
                broken = true;
                return 0;
            }

            //Log.d(TAG, "setupOutputTexture: unbinding texture");
            GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, 0);
            if (checkGlError("glBindTexture 0")) {
                broken = true;
                return 0;
            }

        }

        //Log.d(TAG, "setupOutputTexture: binding texture outputTextureHandle: " + outputTextureHandle);
        GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, outputTextureHandle);
        if (checkGlError("glBindTexture")) {
            broken = true;
            return 0;
        }
        //Log.d(TAG, "setupOutputTexture: bound texture outputTextureHandle: " + outputTextureHandle);

        //Log.d(TAG, "setupOutputTexture: texImage2D framebufferWidth: " + framebufferWidth + " framebufferHeight: " + framebufferHeight);
        int format = GLES20.GL_RGBA;
        GLES20.glTexImage2D(GLES20.GL_TEXTURE_2D, 0, format, framebufferWidth, framebufferHeight, 0, format, GLES20.GL_UNSIGNED_BYTE, null);
        if (checkGlError("glTexImage2D")) {
            broken = true;
            return 0;
        }

        //Log.d(TAG, "setupOutputTexture: unbinding texture");
        GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, 0);
        if (checkGlError("glBindTexture 0")) {
            broken = true;
            return 0;
        }

        if (framebufferHandle != 0) {

            // Delete the old framebuffer.

            //Log.d(TAG, "setupOutputTexture: deleting old framebufferHandle: " + framebufferHandle);
            int[] handles = { framebufferHandle };
            GLES20.glDeleteFramebuffers(1, handles, 0);
            if (checkGlError("glDeleteFramebuffers")) {
                broken = true;
                return 0;
            }
            //Log.d(TAG, "setupOutputTexture: deleted old framebuffer");

            framebufferHandle = 0;

        }

        surfaceTexture.setDefaultBufferSize(framebufferWidth, framebufferHeight);

        int[] handles = { 0 };
        GLES20.glGenFramebuffers(1, handles, 0);
        if (checkGlError("glGenFramebuffers")) {
            broken = true;
            return 0;
        }
        framebufferHandle = handles[0];
        //Log.d(TAG, "setupOutputTexture: generated framebuffer framebufferHandle: " + framebufferHandle);

        GLES20.glBindFramebuffer(GLES20.GL_FRAMEBUFFER, framebufferHandle);
        if (checkGlError("glBindFramebuffer")) {
            broken = true;
            return 0;
        }
        //Log.d(TAG, "setupOutputTexture: bound framebuffer framebufferHandle: " + framebufferHandle);

        GLES20.glFramebufferTexture2D(GLES20.GL_FRAMEBUFFER, GLES20.GL_COLOR_ATTACHMENT0, GLES20.GL_TEXTURE_2D, outputTextureHandle, 0);
        if (checkGlError("glFramebufferTexture2D")) {
            broken = true;
            return 0;
        }
        //Log.d(TAG, "setupOutputTexture: glFramebufferTexture2D outputTextureHandle: " + outputTextureHandle);

        int framebufferStatus = GLES20.glCheckFramebufferStatus(GLES20.GL_FRAMEBUFFER);
        if (checkGlError("glCheckFramebufferStatus")) {
            broken = true;
            return 0;
        }
        //Log.d(TAG, "setupOutputTexture: framebufferStatus: " + framebufferStatus);

        return outputTextureHandle;
    }
    

    public void shutDown()
    {
        if (!initialized) {
            return;
        }

        initialized = false;

        surface = null;

        if (surfaceTexture != null) {
            //Log.d(TAG, "shutDown: releasing surfaceTexture: " + surfaceTexture);
            surfaceTexture.release();
            surfaceTexture = null;
        }

        if (vertexBufferHandle != 0) {
            //Log.d(TAG, "shutDown: deleting vertexBufferHandle: " + vertexBufferHandle);
            int[] handles = { vertexBufferHandle };
            GLES20.glDeleteBuffers(1, handles, 0);
            if (checkGlError("glDeleteBuffers")) {
                broken = true;
            }
            vertexBufferHandle = 0;
        }

        if (programHandle != 0) {
            //Log.d(TAG, "shutDown: deleting programHandle: " + programHandle);
            GLES20.glDeleteProgram(programHandle);
            if (checkGlError("glDeleteProgram")) {
                broken = true;
            }
            programHandle = 0;
        }

        if (externalTextureHandle != 0) {
            //Log.d(TAG, "shutDown: deleting externalTextureHandle: " + externalTextureHandle);
            int[] handles = { externalTextureHandle };
            GLES20.glDeleteTextures(1, handles, 0);
            if (checkGlError("glDeleteTextures")) {
                broken = true;
            }
            externalTextureHandle = 0;
        }

        if (framebufferHandle != 0) {
            //Log.d(TAG, "shutDown: deleting framebufferHandle: " + framebufferHandle);
            int[] handles = { framebufferHandle };
            GLES20.glDeleteFramebuffers(1, handles, 0);
            if (checkGlError("glDeleteFramebuffers")) {
                broken = true;
            }
            framebufferHandle = 0;
        }

        if (outputTextureHandle != 0) {
            //Log.d(TAG, "shutDown: deleting outputTextureHandle: " + outputTextureHandle);
            int[] handles = { outputTextureHandle };
            GLES20.glDeleteTextures(1, handles, 0);
            if (checkGlError("glDeleteTextures")) {
                broken = true;
            }
            outputTextureHandle = 0;
        }

    }


    // SurfaceTexture.OnFrameAvailableListener
    synchronized public void onFrameAvailable(SurfaceTexture surface)
    {
        //Log.d(TAG, "onFrameAvailable: frameAvailable: " + frameAvailable + " => true");
        frameAvailable = true;
    }


    public boolean update()
    {
        //Log.d(TAG, "update: initialized: " + initialized + " broken: " + broken + " frameAvailable: " + frameAvailable);

        initialize();

        if (broken || !frameAvailable) {
            return false;
        }

        frameAvailable = false;

        //Log.d(TAG, "update: frameAvailable so will updateTexImage");

        surfaceTexture.updateTexImage();

        long surfaceTextureTimestamp = surfaceTexture.getTimestamp();
        if (surfaceTextureTimestamp == lastTimestamp) {
            //Log.d(TAG, "update: frameAvailable but no change from lastTimestamp: " + lastTimestamp);
            return false;
        }
        lastTimestamp = surfaceTextureTimestamp;

        //Log.d(TAG, "update: frameAvailable so updated lastTimestamp: " + lastTimestamp + " ----------------");

        if (framebufferHandle == 0) {
            Log.e(TAG, "No framebufferHandle.");
            broken = true;
            return false;
        }

        //Log.d(TAG, "update: binding framebufferHandle: " + framebufferHandle);
        GLES20.glBindFramebuffer(GLES20.GL_FRAMEBUFFER, framebufferHandle);
        if (checkGlError("glBindFramebuffer")) {
            broken = true;
            return false;
        }

        Log.d(TAG, "update: saving last texture lastExternalTextureHandles");
        int[] lastExternalTextureHandles = new int[1];
        GLES20.glGetIntegerv(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, lastExternalTextureHandles, 0);
        if (checkGlError("glBindTexture glGetIntegerv GL_TEXTURE_EXTERNAL_OES")) {
            Log.d(TAG, "update: glGetIntegerv GL_TEXTURE_EXTERNAL_OES failed");
            //broken = true;
            //return false;
        }
        Log.d(TAG, "update: saved last texture lastExternalTextureHandles " + lastExternalTextureHandles[0]);

        Log.d(TAG, "update: binding texture externalTextureHandle " + externalTextureHandle);
        GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, externalTextureHandle);
        if (checkGlError("glBindTexture externalTextureHandle")) {
            //Log.d(TAG, "update: glBindTexture failed");
            broken = true;
            return false;
        }
        Log.d(TAG, "update: bound texture externalTextureHandle " + externalTextureHandle);

        //Log.d(TAG, "update: disabling stuff");
        GLES20.glDisable(GLES20.GL_DEPTH_TEST);
        GLES20.glDisable(GLES20.GL_SCISSOR_TEST);
        GLES20.glDisable(GLES20.GL_STENCIL_TEST);
        GLES20.glDisable(GLES20.GL_CULL_FACE);
        GLES20.glDisable(GLES20.GL_BLEND);
        GLES20.glDisable(GLES20.GL_POLYGON_OFFSET_FILL);
        GLES20.glPolygonOffset(GLES20.GL_ELEMENT_ARRAY_BUFFER, 0);
        GLES20.glDepthMask(false);
        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, 0);
        GLES20.glBindBuffer(GLES20.GL_ELEMENT_ARRAY_BUFFER, 0);
        if (checkGlError("glDisable")) {
            broken = true;
            return false;
        }

        //Log.d(TAG, "update: glViewport framebufferWidth: " + framebufferWidth + " framebufferHeight: " + framebufferHeight);
        GLES20.glViewport(0, 0, framebufferWidth, framebufferHeight);
        if (checkGlError("glViewport")) {
            broken = true;
            return false;
        }

        //Log.d(TAG, "update: glClearColor");
        GLES20.glClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        if (checkGlError("glClearColor")) {
            broken = true;
            return false;
        }

        //Log.d(TAG, "update: glClear");
        GLES20.glClear(GLES20.GL_COLOR_BUFFER_BIT);
        if (checkGlError("glClear")) {
            broken = true;
            return false;
        }

        //Log.d(TAG, "update: calling drawFrame");
        drawFrame();
        //Log.d(TAG, "update: called drawFrame");

        //Log.d(TAG, "update: glFinish");
        GLES20.glFinish();
        if (checkGlError("glFinish")) {
            broken = true;
            return false;
        }

        // Generate mipmap?

        //Log.d(TAG, "update: glUseProgram 0");
        GLES20.glUseProgram(0);
        if (checkGlError("glUseProgram 0")) {
            broken = true;
            return false;
        }

        //Log.d(TAG, "update: glBindFramebuffer 0");
        GLES20.glBindFramebuffer(GLES20.GL_FRAMEBUFFER, 0);
        if (checkGlError("glBindFramebuffer 0")) {
            broken = true;
            return false;
        }

        Log.d(TAG, "initialize: restored binding texture lastExternalTextureHandles" + lastExternalTextureHandles[0]);
        GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, lastExternalTextureHandles[0]);
        if (checkGlError("glBindTexture lastExternalTextureHandles")) {
            Log.d(TAG, "initialize: glBindTexture lastExternalTextureHandles failed");
            //broken = true;
            //return false;
        }
        Log.d(TAG, "initialize: restored bound texture lastExternalTextureHandles " + lastExternalTextureHandles[0]);

        //Log.d(TAG, "update: glBindTexture 0");
        GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, 0);
        if (checkGlError("glBindTexture 0")) {
            broken = true;
            return false;
        }

        //Log.d(TAG, "update: glActiveTexture GL_TEXTURE0");
        GLES20.glActiveTexture(GLES20.GL_TEXTURE0);
        if (checkGlError("glActiveTexture GL_TEXTURE0")) {
            broken = true;
            return false;
        }

        //Log.d(TAG, "update: glBindTexture 0");
        GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, 0);
        if (checkGlError("glBindTexture 0")) {
            broken = true;
            return false;
        }

        //Log.d(TAG, "update: glActiveTexture GL_TEXTURE1");
        GLES20.glActiveTexture(GLES20.GL_TEXTURE1);
        if (checkGlError("glActiveTexture GL_TEXTURE1")) {
            broken = true;
            return false;
        }

        //Log.d(TAG, "update: glBindTexture 0");
        GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, 0);
        if (checkGlError("glBindTexture 0")) {
            broken = true;
            return false;
        }

        //Log.d(TAG, "update: done");

        return true;
    }


    public void drawFrame()
    {
        //Log.d(TAG, "drawFrame: surfaceTexture: " + surfaceTexture);

        if (checkGlError("drawFrame start")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glUseProgram: programHandle: " + programHandle);
        GLES20.glUseProgram(programHandle);
        if (checkGlError("glUseProgram")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glActiveTexture GL_TEXTURE0");
        GLES20.glActiveTexture(GLES20.GL_TEXTURE0);
        if (checkGlError("glActiveTexture GL_TEXTURE0")) {
            broken = true;
            return;
        }

        Log.d(TAG, "drawFrame: saving last texture lastExternalTextureHandles");
        int[] lastExternalTextureHandles = new int[1];
        GLES20.glGetIntegerv(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, lastExternalTextureHandles, 0);
        if (checkGlError("glBindTexture glGetIntegerv GL_TEXTURE_EXTERNAL_OES")) {
            Log.d(TAG, "drawFrame: glGetIntegerv GL_TEXTURE_EXTERNAL_OES failed");
            //broken = true;
            //return;
        }
        Log.d(TAG, "drawFrame: saved last texture lastExternalTextureHandles " + lastExternalTextureHandles[0]);

        Log.d(TAG, "drawFrame: glBindTexture GL_TEXTURE_EXTERNAL_OES externalTextureHandle: " + externalTextureHandle);
        GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, externalTextureHandle);
        if (checkGlError("glBindTexture")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glUniformMatrix4fv uMatrix");
        surfaceTexture.getTransformMatrix(uMatrix);
        GLES20.glUniformMatrix4fv(uMatrixHandle, 1, false, uMatrix, 0);
        if (checkGlError("glUniformMatrix4fv uMatrix")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glEnableVertexAttribArray aPositionHandle: " + aPositionHandle);
        GLES20.glEnableVertexAttribArray(aPositionHandle);
        if (checkGlError("glEnableVertexAttribArray aPositionHandle")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glBindBuffer GL_ARRAY_BUFFER vertexBufferHandle: " + vertexBufferHandle);
        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, vertexBufferHandle);
        if (checkGlError("glBindBuffer vertexBufferHandle: " + vertexBufferHandle)) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glVertexAttribPointer aPositionHandle: " + aPositionHandle);
        GLES20.glVertexAttribPointer(aPositionHandle, 3, GLES20.GL_FLOAT, false, VERTICES_DATA_STRIDE_BYTES, VERTICES_DATA_POS_OFFSET_BYTES);
        if (checkGlError("glVertexAttribPointer maPosition")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glEnableVertexAttribArray aTextureHandle: " + aTextureHandle);
        GLES20.glEnableVertexAttribArray(aTextureHandle);
        if (checkGlError("glEnableVertexAttribArray aTextureHandle")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glBindBuffer GL_ARRAY_BUFFER vertexBufferHandle: " + vertexBufferHandle);
        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, vertexBufferHandle);
        if (checkGlError("glBindBuffer vertexBufferHandle: " + vertexBufferHandle)) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glVertexAttribPointer aTextureHandle: " + aTextureHandle);
        GLES20.glVertexAttribPointer(aTextureHandle, 2, GLES20.GL_FLOAT, false, VERTICES_DATA_STRIDE_BYTES, VERTICES_DATA_UV_OFFSET_BYTES);
        if (checkGlError("glVertexAttribPointer aTextureHandle")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glDrawArrays");
        GLES20.glDrawArrays(GLES20.GL_TRIANGLE_STRIP, 0, 4);
        if (checkGlError("glDrawArrays")) {
            broken = true;
            return;
        }

        GLES20.glDisableVertexAttribArray(aPositionHandle);
        if (checkGlError("glDisableVertexAttribArray aPositionHandle")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glDisableVertexAttribArray aTextureHandle: " + aTextureHandle);
        GLES20.glDisableVertexAttribArray(aTextureHandle);
        if (checkGlError("glDisableVertexAttribArray aTextureHandle")) {
            broken = true;
            return;
        }

        //Log.d(TAG, "drawFrame: glBindBuffer GL_ARRAY_BUFFER 0");
        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, 0);
        if (checkGlError("glBindBuffer 0")) {
            broken = true;
            return;
        }

        Log.d(TAG, "drawFrame: restoring binding texture lastExternalTextureHandles " + lastExternalTextureHandles[0]);
        GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, lastExternalTextureHandles[0]);
        if (checkGlError("glBindTexture lastExternalTextureHandles")) {
            Log.d(TAG, "drawFrame: glBindTexture restoring lastExternalTextureHandles failed");
            //broken = true;
            //return;
        }
        Log.d(TAG, "drawFrame: restoring bound texture lastExternalTextureHandles " + lastExternalTextureHandles[0]);

        ////////////////////////////////////////////////////////////////////////

        //Log.d(TAG, "drawFrame: done");

    }


    private int createProgram(String vertexSource, String fragmentSource)
    {

        int vertexShader = loadShader(GLES20.GL_VERTEX_SHADER, vertexSource);
        if (vertexShader == 0) {
            Log.e(TAG, "createProgram: Could not load vertex shader.");
            broken = true;
            return 0;
        }
        //Log.d(TAG, "createProgram: Loaded vertex shader.");

        int pixelShader = loadShader(GLES20.GL_FRAGMENT_SHADER, fragmentSource);
        if (pixelShader == 0) {
            Log.e(TAG, "createProgram: Could not load pixel shader.");
            broken = true;
            return 0;
        }
        //Log.d(TAG, "createProgram: Loaded pixel shader.");

        int program = GLES20.glCreateProgram();
        if (program == 0) {
            Log.e(TAG, "createProgram: Could not create shader program.");
            broken = true;
            return 0;
        }
        //Log.d(TAG, "createProgram: Created program.");

        GLES20.glAttachShader(program, vertexShader);
        if (checkGlError("glAttachShader vertexShader")) {
            Log.e(TAG, "Could not attach vertexShader.");
            broken = true;
            return 0;
        }
        //Log.d(TAG, "createProgram: Attached vertexShader.");

        GLES20.glAttachShader(program, pixelShader);
        if (checkGlError("glAttachShader pixelShader")) {
            Log.e(TAG, "Could not attach pixelShader.");
            broken = true;
            return 0;
        }
        //Log.d(TAG, "createProgram: Attached pixelShader.");

        GLES20.glLinkProgram(program);
        if (checkGlError("glLinkProgram")) {
            Log.e(TAG, "Could not link program.");
            broken = true;
            return 0;
        }
        int[] linkStatus = new int[1];
        GLES20.glGetProgramiv(program, GLES20.GL_LINK_STATUS, linkStatus, 0);
        if (checkGlError("glGetProgramiv") ||
                (linkStatus[0] != GLES20.GL_TRUE)) {
            Log.e(TAG, "Could not link program: ");
            Log.e(TAG, GLES20.glGetProgramInfoLog(program));
            GLES20.glDeleteProgram(program);
            broken = true;
            return 0;
        }
        //Log.d(TAG, "createProgram: Linked program.");

        return program;
    }


    private int loadShader(int shaderType, String source)
    {
        int shader = GLES20.glCreateShader(shaderType);
        if (checkGlError("glCreateShader") ||
            (shader == 0)) {
            Log.e(TAG, "loadShader: glError: Could not create shaderType: " + shaderType);
            broken = true;
            return 0;
        }
        //Log.d(TAG, "loadShader: created shader.");

        GLES20.glShaderSource(shader, source);
        if (checkGlError("glShaderSource")) {
            Log.e(TAG, "loadShader: glError: Could not set shader source. shader: " + shader + " source: " + source);
            broken = true;
            return 0;
        }
        //Log.d(TAG, "loadShader: Set shader source.");

        GLES20.glCompileShader(shader);
        if (checkGlError("glCompileShader")) {
            Log.e(TAG, "loadShader: glError: Could not compile shader. shaderType: " + shaderType + " shader: " + shader);
            broken = true;
            return 0;
        }
        int[] compiled = new int[1];
        GLES20.glGetShaderiv(shader, GLES20.GL_COMPILE_STATUS, compiled, 0);
        if (checkGlError("glGetShaderiv") ||
            (compiled[0] == 0)) {
            Log.e(TAG, "loadShader: Could not compile shader. shaderType: " + shaderType);
            Log.e(TAG, GLES20.glGetShaderInfoLog(shader));
            GLES20.glDeleteShader(shader);
            return 0;
        }
        //Log.d(TAG, "loadShader: Compiled shader.");

        return shader;
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
