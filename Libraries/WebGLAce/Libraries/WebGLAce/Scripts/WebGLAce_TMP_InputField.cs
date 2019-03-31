using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;


public class WebGLAce_TMP_InputField: TMP_InputField
{


    public string editorConfigScript = "";
    public bool showWhenSelected = true;
    public int editorID = -1;
    public int padding = 1;


#if UNITY_WEBGL


    private void GetScreenRect(out int x, out int y, out int width, out int height)
    {
        // Get our RectTransform.
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        //Debug.Log("WebGLAce_TMP_InputField: GetScreenRect: rt: " + rt);

        // Climb up to get the Canvas of the RectTransform.
        Transform parentTransform = rt.parent;
        Canvas canvas = null;
        while (true) {

            // Do we have a canvas?
            canvas = parentTransform.gameObject.GetComponent<Canvas>();
            //Debug.Log("WebGLAce_TMP_InputField: GetScreenRect: parentTransform: " + parentTransform + " canvas: " + canvas);
            if (canvas != null) {
                break;
            }

            // Climb up to the parent.
            parentTransform = parentTransform.parent;
            if (parentTransform == null) {
                Debug.LogError("WebGLAce_TMP_InputField: GetScreenRect: missing canvas parent!");
                x = 0; y = 0; width = 0; height = 0;
                return;
            }

        }

        // Get the size of the Canvas's RectTransform.
        RectTransform rtCanvas = canvas.gameObject.GetComponent<RectTransform>();
        float canvasWidth = rtCanvas.sizeDelta.x;
        float canvasHeight = rtCanvas.sizeDelta.y;

        // Transform the corners of our RectTransform from world coordinates to canvas local coordinates.
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        for (int i = 0; i < 4; i++) {
            corners[i] = rtCanvas.InverseTransformPoint(corners[i]);
        }

        // The corners are relative to the upside-down middle of the canvas, so offset and flip the position, and convert to integers.
        x =   (int)((canvasWidth  * 0.5f) + corners[0].x) + padding;
        y =   (int)((canvasHeight * 0.5f) - corners[2].y) + padding;
        width =  (int)(corners[2].x - corners[0].x) - (padding * 2);
        height = (int)(corners[2].y - corners[0].y) - (padding * 2);
    }


    public void UpdateEditor()
    {
        if (editorID < 0) {
            return;
        }

        WebGLAce.SetEditorReadOnly(editorID, !this.interactable);
        WebGLAce.SetEditorFocused(editorID, this.interactable && this.enabled);
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = !(this.interactable && this.enabled);
#endif
        Debug.Log("WebGLAce_TMP_InputField: UpdateEditor: readOnly " + (!this.interactable) + " focused: " + (this.interactable && this.enabled) + " capture: " + !(this.interactable && this.enabled));
    }


    public void ShowEditor()
    {
        if (editorID < 0) {
            int x, y, width, height;
            GetScreenRect(out x, out y, out width, out height);
            //Debug.Log("WebGLAce_TMP_InputField: ShowEditor: screenRect " + x + " " + y + " " + width + " " + height);

            editorID = WebGLAce.CreateEditor(x, y, width, height, this.text, this.editorConfigScript);
        } else {
            WebGLAce.SetEditorText(editorID, this.text);
        }

        WebGLAce.SetEditorVisible(editorID, true);
        UpdateEditor();
    }


    public void HideEditor()
    {
        if (editorID < 0) {
            return;
        }

        if (this.interactable) {
            string text = WebGLAce.GetEditorText(editorID);
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("WebGLAce_TMP_InputField: HideEditor: text: " + text);
            this.text = text;
#endif
        }

        WebGLAce.SetEditorVisible(editorID, false);
        WebGLAce.SetEditorFocused(editorID, false);
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = true;
#endif
    }


    public void OnEnable()
    {
        Debug.Log("WebGLAce_TMP_InputField: OnEnable", this);

        if (!showWhenSelected) {
            ShowEditor();
        }

        base.OnEnable();
    }


    public void OnDisable()
    {
        Debug.Log("WebGLAce_TMP_InputField: OnDisable", this);

        HideEditor();

        base.OnDisable();
    }


    public override void OnSelect(BaseEventData data)
    {
        Debug.Log("WebGLAce_TMP_InputField: OnSelect", this);

        if (showWhenSelected) {
            ShowEditor();
        }

        base.OnSelect(data);
    }


    public override void OnDeselect(BaseEventData data)
    {
        Debug.Log("WebGLAce_TMP_InputField: OnDeselect", this);

        if (showWhenSelected) {
            HideEditor();
        }

        base.OnDeselect(data);
    }


    public void OnRectTransformDimensionsChange()
    {
        Debug.Log("WebGLAce_TMP_InputField: OnRectTransformDimensionsChange", this);

        if (editorID >= 0) {
            int x, y, width, height;
            GetScreenRect(out x, out y, out width, out height);
            WebGLAce.ResizeEditor(editorID, x, y, width, height);
        }
    }


    public void OnDestroy()
    {
        Debug.Log("WebGLAce_TMP_InputField: OnDestroy", this);

        if (editorID >= 0) {
            WebGLAce.DestroyEditor(editorID);
            editorID = -1;
        }
    }


    public string GetEditorText()
    {
        if (editorID >= 0) {
            return WebGLAce.GetEditorText(editorID);
        } else {
            return this.text;
        }
    }


    public void SetEditorText(string text)
    {
        if (editorID >= 0) {
            WebGLAce.SetEditorText(editorID, text);
        }
    }


    public void ConfigureEditor(string configScript)
    {
        if (editorID >= 0) {
            WebGLAce.ConfigureEditor(editorID, configScript);
        }
    }


#else

    public void UpdateEditor()
    {
    }


#endif


}
