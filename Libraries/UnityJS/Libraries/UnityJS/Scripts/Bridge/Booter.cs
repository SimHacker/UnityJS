////////////////////////////////////////////////////////////////////////
// Booter.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public enum BooterPanelState {
    Boot,
    Edit,
    Question,
};


public enum QuestionAction {
    Delete,
    Duplicate,
};


public enum PropertyType {
    String,
    Boolean,
    Integer,
    Float,
    Script,
};


public struct PropertyData {
    public string key;
    public string name;
    public string description;
    public PropertyType type;
};


public struct ScriptData {
    public string key;
    public string name;
    public string description;
};


public class Booter: MonoBehaviour {


    ////////////////////////////////////////////////////////////////////////
    // Instance Variables


    public Bridge bridge;
    public BooterPanelState booterPanelState;
    public QuestionAction questionAction;
    public string duplicateName;
    public bool updateInterface = false;
    public string currentBootConfigurationIndexKey = "CurrentBootConfigurationIndex";
    public string bootConfigurationsKey = "BootConfigurations";
    public JArray bootConfigurations;
    public int currentBootConfigurationIndex = 0;
    public int currentPropertyIndex = 1;
    public bool bootNow = true;
    public bool bootButtonEnabled = true;
    public int bootMouseButton = 0;
    public Rect bootButtonRect = new Rect(0, 0, 100, 100);
    public bool bootButtonPressed = false;
    public bool bootButtonActivated = false;
    public float bootButtonPressedTime;
    public float bootButtonPressDuration = 1.0f;
    public bool bootCanvasShown = false;
    public JObject currentBootConfiguration;
    public JObject editingBootConfiguration;
    public Canvas bootCanvas;
    public bool firstUpdate = true;
    public RectTransform bootPanel;
    public TMP_Dropdown bootDropdown;
    public Button bootButton;
    public Button duplicateButton;
    public Button cancelButton;
    public Button editButton;
    public RectTransform editPanel;
    public TextMeshProUGUI nameLabel;
    public Button deleteButton;
    public Button revertButton;
    public Button saveButton;
    public RectTransform questionPanel;
    public Button yesButton;
    public TextMeshProUGUI questionLabel;
    public Button noButton;
    public RectTransform propertyPanel;
    public TMP_Dropdown propertyDropdown;
    public TextMeshProUGUI propertyDescriptionLabel;
    public TMP_InputField stringInputField;
    public Toggle booleanToggle;
    public TextMeshProUGUI booleanToggleLabel;
    //public TMP_InputField scriptInputField;
    public WebGLAce_TMP_InputField scriptInputField;
    public List<GameObject> disabledObjects;

    public PropertyData[] propertyDataArray = new PropertyData[] {
        new PropertyData {
            key="name",
            name="Name",
            description="Boot configuration name.",
            type=PropertyType.String,
        },
        new PropertyData { 
            key="description", 
            name="Description", 
            description="Boot configuration description.", 
            type=PropertyType.String,
        },
#if USE_SOCKETIO && UNITY_EDITOR
        new PropertyData {
            key="useSocketIO",
            name="Use SocketIO",
            description="Use SocketIO to connect to JavaScript engine.",
            type=PropertyType.Boolean,
        },
        new PropertyData {
            key="socketIOAddress",
            name="SocketIO Address",
            description="Address of SocketIO server.",
            type=PropertyType.String,
        },
#endif
        new PropertyData {
            key="handleStartedScript",
            name="Handle Started Script",
            description="Called at start, before loading data.",
            type=PropertyType.Script,
        },
        new PropertyData {
            key="handleLoadedScript",
            name="Handle Loaded Script",
            description="Called after loading data.",
            type=PropertyType.Script,
        },
        new PropertyData {
            key="handleLoadFailedScript",
            name="Handle Load Failed Script",
            description="Called if problem loading data.",
            type=PropertyType.Script,
        },
    };


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    void Awake()
    {
        Debug.Log("Booter: Awake");

        //ResetBootConfigurations();

        bootCanvas.gameObject.SetActive(bootCanvasShown);
        LoadBootConfigurations();
    }
    

    void Update()
    {
        if (firstUpdate) {
            firstUpdate = false;

            bool resetKey = 
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            Debug.Log("Booter: Update: firstUpdate: resetKey: " + resetKey);

            if (resetKey) {
                ResetBootConfigurations();
            }
        }

        if (updateInterface) {
            updateInterface = false;
            UpdateInterface();
        }

        if (bootButtonEnabled &&
            !bootCanvasShown) {

            if (!bootButtonPressed) {

                if (Input.GetMouseButtonDown(bootMouseButton) &&
                    bootButtonRect.Contains(Input.mousePosition)) {

                    bootButtonPressed = true;
                    bootButtonActivated = false;
                    bootButtonPressedTime = Time.time;

                }

            } else { // bootButtonPressed

                if (Input.GetMouseButtonUp(bootMouseButton)) {

                    if (bootButtonActivated &&
                        bootButtonRect.Contains(Input.mousePosition)) {

                        if (Input.GetKey(KeyCode.LeftControl) ||
                            Input.GetKey(KeyCode.RightControl)) {

                            ResetBootConfigurations();

                        }

                        ShowBootCanvas();

                    }

                    bootButtonPressed = false;
                    bootButtonActivated = false;

                } else if (Input.GetMouseButton(bootMouseButton)) {

                    if (Time.time >= (bootButtonPressedTime + bootButtonPressDuration)) {

                        bootButtonActivated = true;

                    }

                } else {

                    bootButtonPressed = false;

                }

            }

        }

        if (bootNow) {
            bootNow = false;

            BootNow();
        }
    }


    public void UpdateInterface()
    {
        Debug.Log("Booter: UpdateInterface: bootDropdown.value: " + bootDropdown.value);

        bootDropdown.options.Clear();

        if (currentBootConfiguration == null) {
            return;
        }

        //Debug.Log("Booter: UpdateInterface: bootConfigurations: " + bootConfigurations);
        foreach (JObject bootConfiguration in bootConfigurations) {

            string name = (string)bootConfiguration["name"];
            if (name == null) {
                name = "";
            }

            //Debug.Log("Booter: UpdateInterface: name: " + name + " bootConfiguration: " + bootConfiguration);

            bootDropdown.options.Add(new TMP_Dropdown.OptionData() {text=name});
        }

        int index = PlayerPrefs.GetInt(currentBootConfigurationIndexKey, 0);
        //Debug.Log("Booter: UpdateInterface: got currentBootConfigurationIndex: " + currentBootConfigurationIndex + " bootConfigurations.Count: " + bootConfigurations.Count);
        if (index < 0) {
            index = 0;
        } else if (index >= bootConfigurations.Count) {
            index = bootConfigurations.Count - 1;
        }

        PlayerPrefs.SetInt(currentBootConfigurationIndexKey, currentBootConfigurationIndex);

        currentBootConfiguration = (JObject)bootConfigurations[currentBootConfigurationIndex];

        //Debug.Log("Booter: UpdateInterface: currentBootConfigurationIndex: " + currentBootConfigurationIndex + " currentBootConfiguration: " + currentBootConfiguration);

        bootDropdown.value = currentBootConfigurationIndex;
        bootDropdown.RefreshShownValue();

        propertyDropdown.options.Clear();

        foreach (PropertyData propertyData in propertyDataArray) {
            propertyDropdown.options.Add(new TMP_Dropdown.OptionData() {text=propertyData.name});
        }

        propertyDropdown.value = currentPropertyIndex;
        propertyDropdown.RefreshShownValue();

        UpdateInterfaceConfiguration();
    }


    public void UpdateInterfaceConfiguration()
    {
        //Debug.Log("Booter: UpdateInterfaceConfiguration: bootDropdown.value: " + bootDropdown.value);

        if (currentBootConfiguration == null) {
            Debug.LogError("Booter: UpdateInterfaceConfiguration: null bootConfiguration!");
            return;
        }

        PropertyData propertyData = propertyDataArray[currentPropertyIndex];

        JObject bootConfiguration =
            (booterPanelState == BooterPanelState.Edit)
                ? editingBootConfiguration
                : currentBootConfiguration;

        if (booterPanelState == BooterPanelState.Edit) {

            nameLabel.text =
                ((editingBootConfiguration == null) ||
                 !editingBootConfiguration.ContainsKey("name"))
                    ? ""
                    : (string)editingBootConfiguration["name"];

        }

        propertyDescriptionLabel.text = propertyData.description;

        switch (propertyData.type) {

            case PropertyType.String: {
                string propertyText = (string)bootConfiguration[propertyData.key];
                if (propertyText == null) {
                    propertyText = "";
                }
                stringInputField.text = propertyText;
                stringInputField.gameObject.SetActive(true);
                booleanToggle.gameObject.SetActive(false);
                scriptInputField.gameObject.SetActive(false);
                break;
            }

            case PropertyType.Boolean: {
                bool propertyBoolean =
                    bootConfiguration.ContainsKey(propertyData.key)
                        ? (bool)bootConfiguration[propertyData.key]
                        : false;
                booleanToggle.isOn = propertyBoolean;
                booleanToggleLabel.text = propertyData.name;
                stringInputField.gameObject.SetActive(false);
                booleanToggle.gameObject.SetActive(true);
                scriptInputField.gameObject.SetActive(false);
                break;
            }

            case PropertyType.Integer: {
                int propertyInt =
                    bootConfiguration.ContainsKey(propertyData.key)
                        ? (int)bootConfiguration[propertyData.key]
                        : 0;
                stringInputField.text = "" + propertyInt;
                stringInputField.gameObject.SetActive(true);
                booleanToggle.gameObject.SetActive(false);
                break;
            }

            case PropertyType.Float: {
                float propertyFloat =
                    bootConfiguration.ContainsKey(propertyData.key)
                        ? (float)bootConfiguration[propertyData.key]
                        : 0.0f;
                stringInputField.text = "" + propertyFloat;
                stringInputField.gameObject.SetActive(true);
                booleanToggle.gameObject.SetActive(false);
                scriptInputField.gameObject.SetActive(false);
                break;
            }

            case PropertyType.Script: {
                string propertyText = (string)bootConfiguration[propertyData.key];
                if (propertyText == null) {
                    propertyText = "";
                }
                scriptInputField.text = propertyText;
                stringInputField.gameObject.SetActive(false);
                booleanToggle.gameObject.SetActive(false);
                scriptInputField.gameObject.SetActive(true);
                break;
            }

        }

        UpdateInterfacePanels();
    }


    public void UpdateInterfacePanels()
    {
        bootPanel.gameObject.SetActive(booterPanelState == BooterPanelState.Boot);
        editPanel.gameObject.SetActive(booterPanelState == BooterPanelState.Edit);
        questionPanel.gameObject.SetActive(booterPanelState == BooterPanelState.Question);
        propertyPanel.gameObject.SetActive(booterPanelState != BooterPanelState.Question);

        bool configurationEditable =
            (currentBootConfiguration.ContainsKey("editable")) &&
            (bool)currentBootConfiguration["editable"];
        bool editable =
            (booterPanelState == BooterPanelState.Edit) &&
            configurationEditable;
        bool deletable =  
            editable && 
            (bootConfigurations.Count > 1);

        saveButton.interactable = editable;
        editButton.interactable = configurationEditable;
        deleteButton.interactable = deletable;
        stringInputField.interactable = editable;
        booleanToggle.interactable = editable;
        scriptInputField.interactable = editable;
        scriptInputField.UpdateEditor();
    }


    public int FindBootConfigurationIndex(string name)
    {
        for (var i = 0; i < bootConfigurations.Count; i++) {
            JObject bootConfiguration = (JObject)bootConfigurations[i];

            if (name == (string)bootConfiguration["name"]) {
                return i;
            }
        }

        return -1;
    }


    public void BootNow()
    {
        Debug.Log("Booter: BootNow: currentBootConfigurationIndex: " + currentBootConfigurationIndex + " currentBootConfiguration: " + currentBootConfiguration);

        if (currentBootConfiguration == null) {

            Debug.Log("Booter: BootNow: currentBootConfiguration is null!");

        } else {

#if USE_SOCKETIO && UNITY_EDITOR

            bool useSocketIO = 
                currentBootConfiguration.ContainsKey("useSocketIO") &&
                (bool)currentBootConfiguration["useSocketIO"];
            Debug.Log("Booter: BootNow: useSocketIO: " + useSocketIO);
            bridge.useSocketIO = useSocketIO;

            string socketIOAddress = (string)currentBootConfiguration["socketIOAddress"];
            if (socketIOAddress == null) {
                socketIOAddress = "";
            }
            Debug.Log("Booter: BootNow: socketIOAddress: " + socketIOAddress);
            bridge.socketIOAddress = socketIOAddress;

#endif

            string handleStartedScript = (string)currentBootConfiguration["handleStartedScript"];
            if (handleStartedScript == null) {
                handleStartedScript = "";
            }
            Debug.Log("Booter: BootNow: handleStartedScript: " + handleStartedScript);
            bridge.handleStartedScript = handleStartedScript;

            string handleLoadedScript = (string)currentBootConfiguration["handleLoadedScript"];
            if (handleLoadedScript == null) {
                handleLoadedScript = "";
            }
            Debug.Log("Booter: BootNow: handleLoadedScript: " + handleLoadedScript);
            bridge.handleLoadedScript = handleLoadedScript;

            string handleLoadFailedScript = (string)currentBootConfiguration["handleLoadFailedScript"];
            if (handleLoadFailedScript != null) {
                handleLoadFailedScript = "";
            }
            Debug.Log("Booter: BootNow: handleLoadFailedScript: " + handleLoadFailedScript);
            bridge.handleLoadFailedScript = handleLoadFailedScript;

        }

        if (bridge.enabled) {
            Debug.Log("Booter: BootNow: bridge was enabled so bridge.Boot()");
            bridge.Boot();
        } else {
            Debug.Log("Booter: BootNow: bridge was disabled so bridge.enabled = true");
            bridge.enabled = true;
        }
    }


    public void HandleBootChanged(int val)
    {
        //Debug.Log("Booter: HandleBootChanged: bootDropdown.itemText: " + bootDropdown.itemText + " val: " + val);

        if (val < 0) {
            return;
        }

        currentBootConfigurationIndex = val;
        currentBootConfiguration = (JObject)bootConfigurations[currentBootConfigurationIndex];
        PlayerPrefs.SetInt(currentBootConfigurationIndexKey, currentBootConfigurationIndex);
        //Debug.Log("Booter: HandleBootChanged: Saved: currentBootConfigurationIndex: " + currentBootConfigurationIndex);

        UpdateInterfaceConfiguration();
    }


    public void HandleBoot()
    {
        HideBootCanvas();
        bootNow = true;
    }


    public void HandleCancel()
    {
        HideBootCanvas();
    }


    public void HandleEdit()
    {
        editingBootConfiguration = (JObject)currentBootConfiguration.DeepClone();

        updateInterface = true;
        booterPanelState = BooterPanelState.Edit;
        UpdateInterfacePanels();
    }


    public void HandleSave()
    {
        bootConfigurations[currentBootConfigurationIndex] = editingBootConfiguration;
        editingBootConfiguration = null;

        updateInterface = true;
        booterPanelState = BooterPanelState.Boot;

        SaveBootConfigurations();
    }


    public void HandleDuplicate()
    {
        duplicateName = (string)currentBootConfiguration["name"];
        if (duplicateName == null) {
            duplicateName = "";
        }

        while (FindBootConfigurationIndex(duplicateName) >= 0) {
            duplicateName += " Copy";
        }

        string question = "Duplicate to \"" + (string)duplicateName + "\"?";
        questionLabel.text = question;

        booterPanelState = BooterPanelState.Question;
        questionAction = QuestionAction.Duplicate;
        UpdateInterfacePanels();
    }


    public void HandleDelete()
    {
        string question = "Delete \"" + (string)currentBootConfiguration["name"] + "\"?";
        questionLabel.text = question;

        booterPanelState = BooterPanelState.Question;
        questionAction = QuestionAction.Delete;
        UpdateInterfacePanels();
    }


    public void HandleRevert()
    {
        editingBootConfiguration = null;

        updateInterface = true;
        booterPanelState = BooterPanelState.Boot;
        UpdateInterfacePanels();
    }


    public void HandleYesButton()
    {
        switch (questionAction) {

            case QuestionAction.Delete:

                if (bootConfigurations.Count > 1) {

                    bootConfigurations.RemoveAt(currentBootConfigurationIndex);

                    if (currentBootConfigurationIndex >= bootConfigurations.Count) {
                        currentBootConfigurationIndex = bootConfigurations.Count - 1;
                    }

                    currentBootConfiguration =
                        (JObject)bootConfigurations[currentBootConfigurationIndex];

                    PlayerPrefs.SetInt(currentBootConfigurationIndexKey, currentBootConfigurationIndex);
                }

                SaveBootConfigurations();

                booterPanelState = BooterPanelState.Boot;
                updateInterface = true;
                break;

            case QuestionAction.Duplicate:

                JObject newBootConfiguration = new JObject();

                foreach (var item in currentBootConfiguration) {
                    string key = item.Key;
                    if (key == "name") {
                        newBootConfiguration[key] = duplicateName;
                    } else if (key == "editable") {
                        newBootConfiguration[key] = true;
                    } else {
                        newBootConfiguration[key] = item.Value;
                    }
                }

                currentBootConfigurationIndex++;
                bootConfigurations.Insert(currentBootConfigurationIndex, newBootConfiguration);

                editingBootConfiguration = (JObject)newBootConfiguration.DeepClone();

                SaveBootConfigurations();

                booterPanelState = BooterPanelState.Edit;
                updateInterface = true;

                break;

        }
    }


    public void HandleNoButton()
    {
        switch (questionAction) {

            case QuestionAction.Delete:
                booterPanelState = BooterPanelState.Edit;
                UpdateInterfacePanels();
                break;

            case QuestionAction.Duplicate:
                booterPanelState = BooterPanelState.Boot;
                UpdateInterfacePanels();
                break;

        }
    }


    public void HandlePropertyChanged(int val)
    {
        //Debug.Log("Booter: HandlePropertyChanged: propertyDropdown.value: " + propertyDropdown.value + " val: " + val);

        currentPropertyIndex = val;

        UpdateInterfaceConfiguration();
    }


    public void HandleStringChanged(string val)
    {
        //Debug.Log("Booter: HandleStringChanged: stringInputField.text: " + stringInputField.text + " val: " + val);

        if (booterPanelState != BooterPanelState.Edit) {
            return;
        }

        PropertyData propertyData = propertyDataArray[currentPropertyIndex];

        switch (propertyData.type) {

            case PropertyType.String: {
                editingBootConfiguration[propertyData.key] = stringInputField.text;
                break;
            }

            case PropertyType.Boolean: {
                editingBootConfiguration[propertyData.key] = booleanToggle.isOn;
                break;
            }

            case PropertyType.Integer: {
                int value = 0;
                Int32.TryParse(stringInputField.text, out value);
                editingBootConfiguration[propertyData.key] = value;
                break;
            }

            case PropertyType.Float: {
                float value = 0.0f;
                Single.TryParse(stringInputField.text, out value);
                editingBootConfiguration[propertyData.key] = value;
                break;
            }

            case PropertyType.Script: {
                editingBootConfiguration[propertyData.key] = scriptInputField.text;
                break;
            }

            default: {
                Debug.LogError("HandleBooleanChanged: expected String, Boolean, Integer, Float or Script propertyData: " + propertyData);
                break;
            }

        }

        // Special case to update the name label when the name is changed.
        if (propertyData.key == "name") {
            nameLabel.text = (string)editingBootConfiguration["name"];
        }

    }


    public void HandleBooleanChanged(bool val)
    {
        //Debug.Log("Booter: HandleBooleanChanged: booleanToggle.isOn: " + booleanToggle.isOn + " val: " + val);

        if (booterPanelState != BooterPanelState.Edit) {
            return;
        }

        PropertyData propertyData = propertyDataArray[currentPropertyIndex];

        switch (propertyData.type) {

            case PropertyType.Boolean: {
                editingBootConfiguration[propertyData.key] = booleanToggle.isOn;
                break;
            }

            default: {
                Debug.LogError("HandleBooleanChanged: expected Boolean propertyData: " + propertyData);
                break;
            }

        }
        
    }


    public void HandleScriptChanged(string val)
    {
        //Debug.Log("Booter: HandleScriptChanged: scriptInputField.text: " + scriptInputField.text + " val: " + val);

        if (booterPanelState != BooterPanelState.Edit) {
            return;
        }

        PropertyData propertyData = propertyDataArray[currentPropertyIndex];

        switch (propertyData.type) {

            case PropertyType.Script: {
                editingBootConfiguration[propertyData.key] = scriptInputField.text;
                break;
            }

            default: {
                Debug.LogError("HandleBooleanChanged: expected Script propertyData: " + propertyData);
                break;
            }

        }
        
    }


    public void ShowBootCanvas()
    {
        if (bootCanvasShown) {
            return;
        }

        GameObject[] rootObjects =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        disabledObjects = new List<GameObject>();

        foreach (GameObject obj in rootObjects) {
            Bridge bridgeComponent = obj.GetComponent<Bridge>();
            if (bridgeComponent != bridge) {
                if (obj.activeSelf) {
                    disabledObjects.Add(obj);
                    obj.SetActive(false);
                }
            }
        }

        bootCanvas.gameObject.SetActive(true);

        booterPanelState = BooterPanelState.Boot;
        updateInterface = true;
        bootCanvasShown = true;
    }
    

    public void HideBootCanvas()
    {
        if (!bootCanvasShown) {
            return;
        }

        bootCanvas.gameObject.SetActive(false);

        if (disabledObjects != null) {
            foreach (GameObject obj in disabledObjects) {
                obj.SetActive(true);
            }
            disabledObjects = null;
        }

        bootCanvasShown = false;
    }
    

    private void LoadBootConfigurations()
    {
        if (bootConfigurations == null) {

            string result = PlayerPrefs.GetString(bootConfigurationsKey, null);

            if (string.IsNullOrEmpty(result)) {

                string fileName = "Config/" + bootConfigurationsKey;
                TextAsset textFile = Resources.Load<TextAsset>(fileName);
                result = textFile.text;
                Resources.UnloadAsset(textFile);
                Debug.Log("Booter: LoadBootConfiguration: fileName: " + fileName + " result: " + result);

            }

            bootConfigurations = (JArray)JToken.Parse(result);

            Debug.Log("Booter: LoadBootConfigurations: bootConfigurationsKey: " + bootConfigurationsKey + " bootConfigurations: " + bootConfigurations);
        }

        if (bootConfigurations == null) {
            bootConfigurations = new JArray();
        }

        currentBootConfigurationIndex = PlayerPrefs.GetInt(currentBootConfigurationIndexKey, 0);
        Debug.Log("Booter: LoadBootConfigurations: got currentBootConfigurationIndex: " + currentBootConfigurationIndex + " bootConfigurations: " + bootConfigurations);

        if (bootConfigurations.Count == 0) {
            currentBootConfigurationIndex = -1;
            currentBootConfiguration = null;
        } else {
           if (currentBootConfigurationIndex < 0) {
               currentBootConfigurationIndex = 0;
           } else if (currentBootConfigurationIndex >= bootConfigurations.Count) {
                currentBootConfigurationIndex = bootConfigurations.Count - 1;
            }
        }

        currentBootConfiguration =
            ((currentBootConfigurationIndex < 0) ||
             (currentBootConfigurationIndex >= bootConfigurations.Count))
                ? null
                : (JObject)bootConfigurations[currentBootConfigurationIndex];

        PlayerPrefs.SetInt(currentBootConfigurationIndexKey, currentBootConfigurationIndex);

        Debug.Log("Booter: LoadBootConfigurations: currentBootConfigurationIndex: " + currentBootConfigurationIndex + " currentBootConfiguration: " + currentBootConfiguration);
    }


    public void SaveBootConfigurations()
    {
        if (bootConfigurations == null) {
            return;
        }

        string data = bootConfigurations.ToString();
        PlayerPrefs.SetString(bootConfigurationsKey, data);
        PlayerPrefs.Save();
    }


    public void ResetBootConfigurations()
    {
        Debug.Log("Booter: ResetBootConfigurations");

        PlayerPrefs.DeleteKey(bootConfigurationsKey);
        PlayerPrefs.DeleteKey(currentBootConfigurationIndexKey);
        PlayerPrefs.Save();

        bootConfigurations = null;
        LoadBootConfigurations();
    }


}


}
