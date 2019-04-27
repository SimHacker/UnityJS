////////////////////////////////////////////////////////////////////////
// BridgeExtensions.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public static class BridgeExtensions {


    // https://stackoverflow.com/questions/299515/reflection-to-identify-extension-methods

    /// <summary>
    /// This Method extends the System.Type-type to get all extended methods.
    // It searches hereby in all assemblies which are known by the current AppDomain.
    /// </summary>
    /// <remarks>
    /// Insired by Jon Skeet from his answer on
    /// http://stackoverflow.com/questions/299515/c-sharp-reflection-to-identify-extension-methods
    /// </remarks>
    /// <returns>returns MethodInfo[] with the extended Method</returns>

    public static MethodInfo[] GetExtensionMethods(this Type t)
    {
        List<Type> AssTypes = new List<Type>();

        foreach (Assembly item in AppDomain.CurrentDomain.GetAssemblies()) {
            AssTypes.AddRange(item.GetTypes());
        }

        var query = from type in AssTypes
            where type.IsSealed && !type.IsGenericType && !type.IsNested
            from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            where method.IsDefined(typeof(ExtensionAttribute), false)
            where method.GetParameters()[0].ParameterType == t
            select method;
        return query.ToArray<MethodInfo>();
    }


    /// <summary>
    /// Extends the System.Type-type to search for a given extended MethodName.
    /// </summary>
    /// <param name="MethodName">Name of the Method</param>
    /// <returns>the found Method or null</returns>
    public static MethodInfo GetExtensionMethod(this Type t, string MethodName)
    {
        var mi = from method in t.GetExtensionMethods()
            where method.Name == MethodName
            select method;

        if (mi.Count<MethodInfo>() <= 0) {
            return null;
        } else {
            return mi.First<MethodInfo>();
        }
    }


    public static bool IsNull(this JToken token)
    {
        return ((token == null) ||
                (token.Type == JTokenType.Null));
    }
    

    public static bool IsUndefined(this JToken token)
    {
        return ((token == null) ||
                (token.Type == JTokenType.Undefined));
    }
    

    public static bool IsString(this JToken token)
    {
        return ((token != null) &&
                (token.Type == JTokenType.String));
    }
    

    public static string GetString(this JToken token, string key, string def=null)
    {
        if ((key == null) ||
            (token == null) ||
            (token.Type != JTokenType.Object)) {
            return def;
        }

        JObject obj = (JObject)token;
        JToken resultToken = obj[key];

        if (resultToken == null) {
            return def;
        }

        string result = def;

        switch (resultToken.Type) {
            case JTokenType.Integer:
            case JTokenType.Float:
            case JTokenType.String:
            case JTokenType.Boolean:
                result = (string)resultToken;
                break;
            default:
                return def;
        }

        return result;
    }


    public static bool IsInteger(this JToken token)
    {
        return ((token != null) &&
                (token.Type == JTokenType.Integer));
    }
    

    public static int GetInteger(this JToken token, string key, int def=0)
    {
        if ((key == null) ||
            (token == null) ||
            (token.Type != JTokenType.Object)) {
            return def;
        }

        JObject obj = (JObject)token;
        JToken resultToken = obj[key];

        if (resultToken == null) {
            return def;
        }

        int result = def;

        switch (resultToken.Type) {
            case JTokenType.Integer:
                result = (int)resultToken;
                break;
            case JTokenType.Float:
                result = (int)(float)resultToken;
                break;
            default:
                return def;
        }

        return result;
    }


    public static bool IsFloat(this JToken token)
    {
        return ((token != null) &&
                (token.Type == JTokenType.Float));
    }
    

    public static float GetFloat(this JToken token, string key, float def=0.0f)
    {
        if ((key == null) ||
            (token == null) ||
            (token.Type != JTokenType.Object)) {
            return def;
        }

        JObject obj = (JObject)token;
        JToken resultToken = obj[key];

        if (resultToken == null) {
            return def;
        }

        float result = def;

        switch (resultToken.Type) {
            case JTokenType.Float:
                result = (float)resultToken;
                break;
            case JTokenType.Integer:
                result = (float)(int)resultToken;
                break;
            default:
                return def;
        }

        return result;
    }


    public static bool IsNumber(this JToken token)
    {
        return ((token != null) &&
                ((token.Type == JTokenType.Integer) ||
                 (token.Type == JTokenType.Float)));
    }
    

    public static bool IsBoolean(this JToken token)
    {
        return ((token != null) &&
                (token.Type == JTokenType.Boolean));
    }
    

    public static bool GetBoolean(this JToken token, string key, bool def=false)
    {
        if ((key == null) ||
            (token == null) ||
            (token.Type != JTokenType.Object)) {
            return def;
        }

        JObject obj = (JObject)token;
        JToken resultToken = obj[key];

        if (resultToken == null) {
            return def;
        }

        bool result = def;

        switch (resultToken.Type) {
            case JTokenType.Boolean:
                result = (bool)resultToken;
                break;
            case JTokenType.Float:
                result = ((float)resultToken) != 0.0f;
                break;
            case JTokenType.Integer:
                result = ((int)resultToken) != 0;
                break;
            case JTokenType.String:
                result = ((string)resultToken) != "";
                break;
            default:
                return def;
        }

        return result;
    }


    public static bool IsArray(this JToken token)
    {
        return ((token != null) &&
                (token.Type == JTokenType.Array));
    }
    

    public static int ArrayLength(this JToken token)
    {
        if (!token.IsArray()) {
            return 0;
        }

        JArray array = (JArray)token;
        return array.Count;
    }
    

    public static JArray GetArray(this JToken token, string key, JArray def=null)
    {
        if ((key == null) ||
            (token == null) ||
            (token.Type != JTokenType.Object)) {
            return def;
        }

        JObject obj = (JObject)token;
        JToken resultToken = obj[key];

        if (resultToken == null) {
            return def;
        }

        JArray result = def;

        switch (resultToken.Type) {
            case JTokenType.Array:
                result = (JArray)resultToken;
                break;
            default:
                return def;
        }

        return result;
    }


    public static bool IsObject(this JToken token)
    {
        return ((token != null) &&
                (token.Type == JTokenType.Object));
    }
    

    public static int ObjectCount(this JToken token)
    {
        if (!token.IsObject()) {
            return 0;
        }

        JObject obj = (JObject)token;
        return obj.Count;
    }


    public static bool ContainsKey(this JToken token, string key)
    {
        if (!token.IsObject()) {
            return false;
        }

        JObject obj = (JObject)token;

        return obj[key] != null;
    }


    public static JObject GetObject(this JToken token, string key, JObject def=null)
    {
        if ((key == null) ||
            (token == null) ||
            (token.Type != JTokenType.Object)) {
            return def;
        }

        JObject obj = (JObject)token;
        JToken resultToken = obj[key];

        if (resultToken == null) {
            return def;
        }

        JObject result = def;

        switch (resultToken.Type) {
            case JTokenType.Object:
                result = (JObject)resultToken;
                break;
            default:
                return def;
        }

        return result;
    }


    // Material.UpdateMaterial takes a JSON object that is interpreted
    // as a magic dictionary containing both fixed key names (like
    // "shader"), and dynamic key names (like "texture_MainTex")
    // starting with a prefix ("texture") and finishing with a dynamic
    // name parameter ("_MainTex").

    // This delegate is called to set a key that begins with prefix
    // and ends with name, by converting the JSON token value to the
    // appropriate type, and calling the appropriate setter or method.

    public delegate bool SetMaterialPrefixDelegate(
        Material material,
        string name,
        JToken token);


    // This record keeps tracks of all the key prefixes of the JSON
    // object used to configure a material. The prefix specifies the
    // key prefix or the entire key, depending on isPrefix. The phase
    // controls the order in which the keys are handled. The setter is
    // called to set the key, passing the material, the name of the
    // key, and a JSON token.

    public class SetMaterialPrefixRecord {

        public string prefix;
        public bool isPrefix;
        public int phase;
        public SetMaterialPrefixDelegate setter;

        public SetMaterialPrefixRecord(string prefix0, bool isPrefix0, int phase0, SetMaterialPrefixDelegate setter0)
        {
            prefix = prefix0;
            isPrefix = isPrefix0;
            phase = phase0;
            setter = setter0;
        }
        
    };


    // The order of the SetMaterialPrefixRecords is important. Make
    // sure the more specific (longer, like textureOffset) prefixes
    // are listed before the root prefixes (shorter, like texture).

    public static List<SetMaterialPrefixRecord> materialKeyPrefixes =
        new List<SetMaterialPrefixRecord>() {

            new SetMaterialPrefixRecord("copyPropertiesFromMaterial", false, 0, delegate(Material material, string name, JToken token) {
                Material otherMaterial = null;
                if (!Bridge.mainBridge.ConvertToType<Material>(token, ref otherMaterial)) {
                    return false;
                }
                material.CopyPropertiesFromMaterial(otherMaterial);
                return true;
            }),

            new SetMaterialPrefixRecord("shader", false, 1, delegate(Material material, string name, JToken token) {
                Shader shader = null;
                if (!Bridge.mainBridge.ConvertToType<Shader>(token, ref shader)) {
                    return false;
                }
                material.shader = shader;
                return true;
            }),

            new SetMaterialPrefixRecord("shaderKeywords", false, 2, delegate(Material material, string name, JToken token) {
                string[] keywords = null;
                if (!Bridge.mainBridge.ConvertToType<string[]>(token, ref keywords)) {
                    return false;
                }
                material.shaderKeywords = keywords;
                return true;
            }),

            new SetMaterialPrefixRecord("doubleSidedGI", false, 3, delegate(Material material, string name, JToken token) {
                bool doubleSidedGI = false;
                if (!Bridge.mainBridge.ConvertToType<bool>(token, ref doubleSidedGI)) {
                    return false;
                }
                material.doubleSidedGI = doubleSidedGI;
                return true;
            }),

            new SetMaterialPrefixRecord("enableInstancing", false, 3, delegate(Material material, string name, JToken token) {
                bool enableInstancing = false;
                if (!Bridge.mainBridge.ConvertToType<bool>(token, ref enableInstancing)) {
                    return false;
                }
                material.enableInstancing = enableInstancing;
                return true;
            }),

            new SetMaterialPrefixRecord("globalIlluminationFlags", false, 3, delegate(Material material, string name, JToken token) {
                MaterialGlobalIlluminationFlags flags = 0;
                if (!Bridge.mainBridge.ConvertToType<MaterialGlobalIlluminationFlags>(token, ref flags)) {
                    return false;
                }
                material.globalIlluminationFlags = flags;
                return true;
            }),

            new SetMaterialPrefixRecord("mainTextureOffset", false, 3, delegate(Material material, string name, JToken token) {
                Vector2 offset = Vector2.zero;
                if (!Bridge.mainBridge.ConvertToType<Vector2>(token, ref offset)) {
                    return false;
                }
                material.mainTextureOffset = offset;
                return true;
            }),

            new SetMaterialPrefixRecord("mainTextureScale", false, 3, delegate(Material material, string name, JToken token) {
                Vector2 scale = Vector2.one;
                if (!Bridge.mainBridge.ConvertToType<Vector2>(token, ref scale)) {
                    return false;
                }
                material.mainTextureScale = scale;
                return true;
            }),

            new SetMaterialPrefixRecord("mainTexture", false, 3, delegate(Material material, string name, JToken token) {
                Texture texture = null;
                if (!Bridge.mainBridge.ConvertToType<Texture>(token, ref texture)) {
                    return false;
                }
                material.mainTexture = texture;
                return true;
            }),

            new SetMaterialPrefixRecord("renderQueue", false, 3, delegate(Material material, string name, JToken token) {
                int renderQueue = 0;
                if (!Bridge.mainBridge.ConvertToType<int>(token, ref renderQueue)) {
                    return false;
                }
                material.renderQueue = renderQueue;
                return true;
            }),

            new SetMaterialPrefixRecord("textureOffset", true, 3, delegate(Material material, string name, JToken token) {
                Vector2 offset = Vector2.zero;
                if (!Bridge.mainBridge.ConvertToType<Vector2>(token, ref offset)) {
                    return false;
                }
                material.SetTextureOffset(name, offset);
                return true;
            }),

            new SetMaterialPrefixRecord("textureScale", true, 3, delegate(Material material, string name, JToken token) {
                Vector2 scale = Vector2.one;
                if (!Bridge.mainBridge.ConvertToType<Vector2>(token, ref scale)) {
                    return false;
                }
                material.SetTextureScale(name, scale);
                return true;
            }),

            new SetMaterialPrefixRecord("texture", true, 3, delegate(Material material, string name, JToken token) {
                Texture texture = null;
                if (!Bridge.mainBridge.ConvertToType<Texture>(token, ref texture)) {
                    return false;
                }
                material.SetTexture(name, texture);
                return true;
            }),

            new SetMaterialPrefixRecord("keyword", true, 3, delegate(Material material, string name, JToken token) {
                bool enabled = false;
                if (!Bridge.mainBridge.ConvertToType<bool>(token, ref enabled)) {
                    return false;
                }
                if (enabled) {
                    material.EnableKeyword(name);
                } else {
                    material.DisableKeyword(name);
                }
                return true;
            }),

            new SetMaterialPrefixRecord("overrideTag", true, 3, delegate(Material material, string name, JToken token) {
                string val = "";
                if (!Bridge.mainBridge.ConvertToType<string>(token, ref val)) {
                    return false;
                }
                material.SetOverrideTag(name, val);
                return true;
            }),

            new SetMaterialPrefixRecord("shaderPass", true, 3, delegate(Material material, string name, JToken token) {
                bool enabled = false;
                if (!Bridge.mainBridge.ConvertToType<bool>(token, ref enabled)) {
                    return false;
                }
                material.SetShaderPassEnabled(name, enabled);
                return true;
            }),

            new SetMaterialPrefixRecord("buffer", true, 3, delegate(Material material, string name, JToken token) {
                ComputeBuffer computeBuffer = null;
                if (!Bridge.mainBridge.ConvertToType<ComputeBuffer>(token, ref computeBuffer)) {
                    return false;
                }
                material.SetBuffer(name, computeBuffer);
                return true;
            }),

            new SetMaterialPrefixRecord("colorArray", true, 3, delegate(Material material, string name, JToken token) {
                Color[] colorArray = null;
                if (!Bridge.mainBridge.ConvertToType<Color[]>(token, ref colorArray)) {
                    return false;
                }
                material.SetColorArray(name, colorArray);
                return true;
            }),

            new SetMaterialPrefixRecord("color", true, 3, delegate(Material material, string name, JToken token) {
                Color color = Color.black;
                if (!Bridge.mainBridge.ConvertToType<Color>(token, ref color)) {
                    return false;
                }
                if (name.Length == 0) {
                    material.color = color;
                } else {
                    material.SetColor(name, color);
                }
                return true;
            }),

            new SetMaterialPrefixRecord("floatArray", true, 3, delegate(Material material, string name, JToken token) {
                float[] floatArray = null;
                if (!Bridge.mainBridge.ConvertToType<float[]>(token, ref floatArray)) {
                    return false;
                }
                material.SetFloatArray(name, floatArray);
                return true;
            }),

            new SetMaterialPrefixRecord("float", true, 3, delegate(Material material, string name, JToken token) {
                float f = 0.0f;
                if (!Bridge.mainBridge.ConvertToType<float>(token, ref f)) {
                    return false;
                }
                material.SetFloat(name, f);
                return true;
            }),

            new SetMaterialPrefixRecord("int", true, 3, delegate(Material material, string name, JToken token) {
                int i = 0;
                if (!Bridge.mainBridge.ConvertToType<int>(token, ref i)) {
                    return false;
                }
                material.SetInt(name, i);
                return true;
            }),

            new SetMaterialPrefixRecord("matrixArray", true, 3, delegate(Material material, string name, JToken token) {
                Matrix4x4[] matrixArray = null;
                if (!Bridge.mainBridge.ConvertToType<Matrix4x4[]>(token, ref matrixArray)) {
                    return false;
                }
                material.SetMatrixArray(name, matrixArray);
                return true;
            }),

            new SetMaterialPrefixRecord("matrix", true, 3, delegate(Material material, string name, JToken token) {
                Matrix4x4 matrix = Matrix4x4.identity;
                if (!Bridge.mainBridge.ConvertToType<Matrix4x4>(token, ref matrix)) {
                    return false;
                }
                material.SetMatrix(name, matrix);
                return true;
            }),

            new SetMaterialPrefixRecord("vectorArray", true, 3, delegate(Material material, string name, JToken token) {
                Vector4[] vectorArray = null;
                if (!Bridge.mainBridge.ConvertToType<Vector4[]>(token, ref vectorArray)) {
                    return false;
                }
                material.SetVectorArray(name, vectorArray);
                return true;
            }),

            new SetMaterialPrefixRecord("vector", true, 3, delegate(Material material, string name, JToken token) {
                Vector4 vector = Vector4.zero;
                if (!Bridge.mainBridge.ConvertToType<Vector4>(token, ref vector)) {
                    return false;
                }
                material.SetVector(name, vector);
                return true;
            }),

        };


    public static void UpdateMaterial(this Material material, JToken materialData)
    {
        //Debug.Log("BridgeExtensions: Material: UpdateMaterial: material: " + material + " materialData: " + materialData.GetType() + " " + materialData);

        JObject obj = materialData as JObject;
        if (obj == null) {
            Debug.LogError("BridgeExtensions: Material: UpdateMaterial: expected object!");
            return;
        }

        // Keys must be handled in order of their phase.

        int phases = 4; // Keep in sync with max phase + 1.
        for (int phase = 0; phase < phases; phase++) {

            foreach (JProperty property in obj.Properties()) {
                string key = property.Name;
                JToken valueToken = (JToken)property.Value;
                //Debug.Log("BridgeExtensions: Material: phase: " + phase + " Key: " + key);

                for (int i = 0, n = materialKeyPrefixes.Count; i < n; i++) {

                    if (materialKeyPrefixes[i].phase != phase) {
                        continue;
                    }

                    string keyPrefix = materialKeyPrefixes[i].prefix;

                    if (key.StartsWith(keyPrefix)) {

                        string name = key.Substring(keyPrefix.Length);
                        if (!materialKeyPrefixes[i].isPrefix &&
                            (name.Length > 0)) {
                            Debug.LogError("BridgeExtensions: UpdateMaterial: key: " + key + " should not have suffix name: " + name);
                        } else {

                            SetMaterialPrefixDelegate setter = materialKeyPrefixes[i].setter;
                            setter(material, name, valueToken);

                        }

                        break;
                    }

                }

            }

        }

    }


    // This method is a work-around for a bug in WebGL that crashes
    // when I just set renderer.materials[] to an array of strings
    // through reflection, which works ok on other platforms.
    public static void SetMaterials(this Renderer renderer, JToken materialsToken)
    {
        Debug.Log("BridgeExtensions: Material: SetMaterials: renderer: " + renderer + " materialsToken: " + materialsToken);

        Material[] materials = null;
        if (!Bridge.mainBridge.ConvertToType<Material[]>(materialsToken, ref materials)) {
            Debug.LogError("BridgeExtensions: SetMaterials: renderer: " + renderer + " expected array of Materials!");
            return;
        }

        //Debug.LogError("BridgeExtensions: SetMaterials: renderer: " + renderer + " BEFORE SET");
        renderer.materials = materials;
        //Debug.LogError("BridgeExtensions: SetMaterials: renderer: " + renderer + " AFTER SET");
    }


    public static Vector3[] GetLinePositions(this LineRenderer lineRenderer)
    {
        //Debug.Log("BridgeExtensions: Material: GetLinePositions: lineRenderer: " + lineRenderer);
        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(positions);
        return positions;
    }


    public static void ResetParticleSystem(this ParticleSystem particleSystem)
    {
        //Debug.Log("BridgeExtensions: ParticleSystem: ResetParticleSystem: particleSystem: " + particleSystem);
        particleSystem.Stop();
        particleSystem.Clear();
        particleSystem.Play();
    }


}


}
