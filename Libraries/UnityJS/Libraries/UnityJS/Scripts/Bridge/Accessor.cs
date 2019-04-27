////////////////////////////////////////////////////////////////////////
// Accessor.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public class Accessor {


    public enum AccessorType {
        Undefined,
        Constant,
        JArray,
        Array,
        List,
        JObject,
        Dictionary,
        Field,
        Property,
        Transform,
        Component,
        Resource,
        Object,
        Method,
    };


    ////////////////////////////////////////////////////////////////////////
    // Instance Variables


    public AccessorType type;
    public bool conditional;
    public bool excited;
    public int index;
    public string str;
    public object obj;
    public object[] objArray;
    public List<object> objList;
    public Dictionary<string, object> objDict;
    public FieldInfo fieldInfo;
    public PropertyInfo propertyInfo;


    ////////////////////////////////////////////////////////////////////////
    // Static Methods


    public static bool FindAccessor(object firstObj, string path, ref Accessor accessor)
    {
        //Debug.Log("Accessor: FindAccessor: firstObj: " + firstObj + " path: " + path);

        if (firstObj == null) {
            Debug.LogError("Accessor: FindAccessor: called with null firstObj for path: " + path);
            accessor = null;
            return false;
        }

        accessor = new Accessor();
        accessor.Init_Constant(firstObj, false, false);

        string[] steps = path.Split('/');

        for (int stepIndex = 0, stepCount = steps.Length;
             stepIndex < stepCount; 
             stepIndex++) {
            string step = steps[stepIndex];

            bool conditional = false;
            bool excited = false;
            bool done = false;
            int chomped = 0;

            // Chomp and remember any suffix punctuation.
            while (!done && (step.Length > 1)) {

                char lastChar = 
                    step[step.Length - chomped - 1];

                switch (lastChar) {
                    case '?':
                        conditional = true;
                        chomped++;
                        break;
                    case '!':
                        excited = true;
                        chomped++;
                        break;
                    default:
                        done = true;
                        break;
                }

            }

            // Chomp off any punctuation.
            if (chomped > 0) {
                step = step.Substring(0, step.Length - chomped);
            }

            // Unescape URL percent encoded characters if necessary.
            if (step.IndexOf('%') != -1) {
                step = Uri.UnescapeDataString(step);
            }

            // Empty steps are not allowed in paths.
            if (step == "") {
                Debug.LogError("Accessor: FindAccessor: empty step in path: " + path);
                return false;
            }

            object nextObj = null;

            // Try to get the next object from the accessor.
            if (!accessor.Get(ref nextObj)) {
                if (!accessor.conditional) {
                    Debug.LogError("Accessor: FindAccessor: error getting accessor: " + accessor + " for path: " + path + " firstObj: " + firstObj + " nextObj: " + nextObj);
                    return false;
                } else {
                    //Debug.Log("Accessor: FindAccessor: ignoring error getting conditional accessor: " + accessor + " for path: " + path + " firstObj: " + firstObj + " nextObj: " + nextObj);
                    accessor.Init_Constant(null, true, false);
                    return true;
                }
            }

            //Debug.Log("Accessor: FindAccessor: Got 1: nextObj: " + nextObj + " == null: " + (nextObj == null) + " equals null: " + nextObj.Equals(null) + " type: " + ((nextObj == null) ? "null" : ("" + nextObj.GetType())) + " accessor: " + accessor + " " + accessor.type + " " + accessor.conditional + " path: " + path + " step: " + step);

            // Handle null results by returning null constant accessor.
            if ((nextObj == null) ||
                nextObj.Equals(null)) {
                //Debug.Log("Accessor: FindAccessor: Got 2: null nextObj: " + nextObj + " accessor: " + accessor + " " + accessor.type + " " + accessor.conditional + " path: " + path + " step: " + step);
                if (!accessor.conditional) {
                    Debug.LogError("Accessor: FindAccessor: null object in path: " + path + " stepIndex: " + stepIndex + " step: " + step + " firstObj: " + firstObj + " nextObj: " + nextObj);
                   return false;
                } else {
                    //Debug.Log("Accessor: FindAccessor: ignoring null from conditional accessor: " + accessor + " for path: " + path + " firstObj: " + firstObj + " nextObj: " + nextObj);
                    accessor.Init_Constant(null, true, false);
                    return true;
                }
            }

            //Debug.Log("Accessor: FindAccessor: Got 3: nextObj: " + nextObj + " accessor: " + accessor + " " + accessor.type + " " + accessor.conditional + " path: " + path + " step: " + step);

            // Parse out the accessor type prefix, defaulting to "member" if not specified.
            string[] parts =
                step.Split(new char[] { ':' }, 2);
            string prefix = 
                (parts.Length == 1)
                    ? "member"
                    : parts[0];
            string rest = 
                parts[parts.Length - 1];

            //Debug.Log("Accessor: FindAccessor: step: " + step + " parts length: " + parts.Length + " prefix: " + prefix + " rest: " + rest);

            // Handle each accessor type prefix.
            // Note that there is not necessarily a 1:1 mapping from accessor prefix to AccessorType.
            // For example:
            // "string:", "float:", "integer:", "int:", "boolean:", "bool:", "null:" and "json:" get a Constant.
            // "property:" gets Property and "field:" gets Field, but "member:" is generic and figures out which to use at runtime.
            // "array:" gets Array and "list:" gets List, "jarray:" get JArray, but "index:" is generic and figures out which to use at runtime.
            // "dict:" and "dictionary:" get Array, "jobject:" gets JObject, but "map:" is generic and figures out which to use at runtime.
            // "dictionary" and "dict" are synonyms for Dictionary.

            switch (prefix) {

                case "string": // Constant string.
                    accessor.Init_Constant(rest, false, false);
                    break;

                case "float": // Constant float.
                    float f = float.Parse(rest);
                    accessor.Init_Constant(f, false, false);
                    break;

                case "int": // Constant int.
                case "integer": // Constant int.
                    int i = int.Parse(rest);
                    accessor.Init_Constant(i, false, false);
                    break;

                case "bool": // Constant bool.
                case "boolean": // Constant bool.
                    bool b = rest.ToLower() == "true";
                    accessor.Init_Constant(b, false, false);
                    break;

                case "null": // Constant null.
                    accessor.Init_Constant(null, false, false);
                    break;

                case "json": // Constant JToken.
                    JToken token = JToken.Parse(rest);
                    accessor.Init_Constant(token, false, false);
                    break;

                case "index": // Array, List or JArray index
                case "jarray": // JArray index
                case "array": // Array index
                case "list": // List index

                    bool searchIndex = prefix == "index";
                    bool searchArray = searchIndex || (prefix == "array");
                    bool searchList = searchIndex || (prefix == "list");
                    bool searchJArray = searchIndex || (prefix == "jarray");
                    int index = 0;

                    bool isInteger = int.TryParse(rest, out index);
                    if (!isInteger) {
                        Debug.LogError("Accessor: FindAccessor: prefix: " + prefix + " expected integer index: " + rest + " path: " + path);
                        accessor = null;
                        return false;
                    }

                    if (searchJArray && (nextObj is JArray)) {

                        JArray jarray = (JArray)nextObj;
                        //Debug.Log("Accessor: FindAccessor: prefix: " + prefix + " jarray: " + jarray + " index: " + index + " path: " + path);
                        if (jarray == null) {
                            Debug.LogError("Accessor: FindAccessor: prefix: " + prefix + " expected JArray: " + rest + " path: " + path);
                            accessor = null;
                            return false;
                        }

                        accessor.Init_JArray(jarray, index, conditional, excited);

                    } else if (searchArray && (nextObj is Array)) {

                        object[] array = (object[])nextObj;
                        //Debug.Log("Accessor: FindAccessor: prefix: " + prefix + " array: " + array + " index: " + index + " path: " + path);
                        if (array == null) {
                            Debug.LogError("Accessor: FindAccessor: prefix: " + prefix + " expected array: " + rest + " path: " + path);
                            accessor = null;
                            return false;
                        }

                        accessor.Init_Array(array, index, conditional, excited);

                    } else if (searchList && (nextObj is List<object>)) {

                        List<object> list = (List<object>)nextObj;
                        if (list == null) {
                            Debug.LogError("Accessor: FindAccessor: prefix: " + prefix + " expected List<object>: " + rest + " path: " + path);
                            accessor = null;
                            return false;
                        }

                        accessor.Init_List(list, index, conditional, excited);

                    } else {
                        Debug.LogError("Accessor: FindAccessor: prefix: " + prefix + " expected indexed array or list nextObj: " + nextObj + " path: " + path);
                        accessor = null;
                        return false;
                    }

                    break;

                case "map": // C# Dictionary<string, object> or JObject key
                case "dict": // C# Dictionary<string, object> key
                case "dictionary": // C# Dictionary<string, object> key
                case "jobject": // JObject key

                    bool searchMap = prefix == "map";
                    bool searchDictionary = searchMap || (prefix == "dict") || (prefix == "dictionary");
                    bool searchJObject = searchMap || (prefix == "jobject");

                    if (searchJObject && (nextObj is JObject)) {

                        JObject jobject = nextObj as JObject;
                        if (jobject == null) {
                            Debug.LogError("Accessor: FindAccessor: prefix: jobject expected JObject: " + rest + " path: " + path + " but got nextObj: " + nextObj + " type: " + nextObj.GetType());
                            accessor = null;
                            return false;
                        }

                        accessor.Init_JObject(jobject, rest, conditional, excited);

                    } else if (searchDictionary && (nextObj is Dictionary<string, object>)) {

                        Dictionary<string, object> dict = nextObj as Dictionary<string, object>;
                        if (dict == null) {
                            Debug.LogError("Accessor: FindAccessor: prefix: dict expected Dictionary<string, object>: " + rest + " path: " + path + " but got nextObj: " + nextObj + " type: " + nextObj.GetType());
                            accessor = null;
                            return false;
                        }

                        accessor.Init_Dictionary(dict, rest, conditional, excited);
                    }

                    break;

                case "transform": // Unity Transform name or index

                    accessor.Init_Transform(nextObj, rest, conditional, excited);

                    break;

                case "component": // Unity MonoBehaviour component class name

                    accessor.Init_Component(nextObj, rest, conditional, excited);

                    break;

                case "resource": // Unity resource path

                    accessor.Init_Resource(rest, conditional, excited);

                    break;

                case "member": // C# object Field or Property name
                case "field": // C# object Field name
                case "property": // C# object Property name

                    FieldInfo fieldInfo = null;
                    PropertyInfo propertyInfo = null;
                    bool searchMember = prefix == "member";
                    bool searchField = searchMember || (prefix == "field");
                    bool searchProperty = searchMember || (prefix == "property");
                    System.Type objectType = nextObj.GetType();
                    System.Type searchType = objectType;

                     //Debug.Log("Accessor: FindAccessor: prefix: " + prefix + " rest: " + rest + " objectType: " + objectType);

                    while (searchType != null) {

#if false
                        Debug.Log ("========================================================================");

                        FieldInfo[] fields = searchType.GetFields(BindingFlags.Public | BindingFlags.Static);
                        Debug.Log ("Accessor: FindAccessor: DUMPING rest: " + rest + " searchType: " + searchType + " fields: " + fields.Length);
                        foreach (FieldInfo fieldInfo1 in fields) {
                            Debug.Log ("Accessor: FindAccessor: fieldInfo1: " + fieldInfo1);
                        }

                        PropertyInfo[] properties = searchType.GetProperties(BindingFlags.Public | BindingFlags.Static);
                        Debug.Log ("Accessor: FindAccessor: DUMPING rest: " + rest + " searchType: " + searchType + " properties: " + properties.Length);
                        foreach (PropertyInfo propertyInfo1 in properties) {
                            Debug.Log ("Accessor: FindAccessor: propertyInfo1: " + propertyInfo1);
                        }

                        Debug.Log ("========================================================================");
#endif

                        if (searchProperty) {
                            fieldInfo = searchType.GetField(rest);
                            //Debug.Log("Accessor: FindAccessor: searching searchType: " + searchType + " for rest: " + rest + " and got fieldInfo: " + fieldInfo);
                            if (fieldInfo != null) {
                                break;
                            }
                        }

                        if (searchField) {
                            propertyInfo = searchType.GetProperty(rest);
                            //Debug.Log("Accessor: FindAccessor: searching searchType: " + searchType + " for rest: " + rest + " and got propertyInfo: " + propertyInfo);
                            if (propertyInfo != null) {
                                break;
                            }
                        }

                        searchType = searchType.BaseType;

                    }

                    if (fieldInfo != null) {
                        //Debug.Log("Accessor: FindAccessor: found fieldInfo: " + fieldInfo + " rest: " + rest);
                        accessor.Init_Field(nextObj, rest, fieldInfo, conditional, excited);
                    } else if (propertyInfo != null) {
                        //Debug.Log("Accessor: FindAccessor: found propertyInfo: " + propertyInfo + " rest: " + rest);
                        accessor.Init_Property(nextObj, rest, propertyInfo, conditional, excited);
                    } else {
                        Debug.LogError("Accessor: FindAccessor: undefined field or property rest: " + rest + " firstObj: " + firstObj + " nextObj: " + nextObj);
                        accessor = null;
                        return false;
                    }

                    break;

                case "object": // Object id

                    accessor.Init_Object(rest, conditional, excited);

                    break;

                case "method": // method name

                    accessor.Init_Method(nextObj, rest, conditional, excited);

                    break;

                default:

                    Debug.LogError("Accessor: FindAccessor: undefined prefix: " + prefix + " in path: " + path);
                    accessor = null;

                    break;

            }

        }

        return true;
    }


    public static bool GetProperty(object target, string name, ref object result)
    {
        //Debug.Log("Accessor: GetProperty: target: " + target + " name: " + name);

        Accessor accessor = null;
        if (!Accessor.FindAccessor(
                target,
                name,
                ref accessor)) {
            Debug.LogError("Accessor: GetProperty: can't find accessor for target: " + target + " name: " + name);
            return false;
        }

        if (!accessor.Get(ref result)) {
            if (!accessor.conditional) {
                Debug.LogError("Accessor: GetProperty: can't get from accessor: " + accessor + " " + accessor.type + " " + accessor.conditional + " for target: " + target + " name: " + name);
                return false;
            } else {
                //Debug.Log("Accessor: GetProperty: conditional accessor returned null: " + accessor + " for target: " + target + " name: " + name);
                result = null;
                return true;
            }
        }

        //Debug.Log("Accessor: GetProperty: target: " + target + " name: " + name + " accessor: " + accessor + " result: " + result);

        return true;
    }


    public static bool SetProperty(object target, string name, JToken jsonValue)
    {
        //Debug.Log("Accessor: SetProperty: target: " + target + " name: " + name + " jsonValue: " + jsonValue);

        Accessor accessor = null;
        if (!Accessor.FindAccessor(
                target,
                name,
                ref accessor)) {
            Debug.LogError("Accessor: SetProperty: can't find accessor for target: " + target + " name: " + name);
            return false;
        }

        object value = null;
        Type targetType = accessor.GetTargetType();
        //Debug.Log("Accessor: SetProperty: accessor: " + accessor + " conditional: " + accessor.conditional + " excited: " + accessor.excited + " targetType: " + targetType);

        if (targetType == null) {
            if (!accessor.conditional) {
                Debug.LogError("Accessor: SetProperty: accessor got null targetType");
                return false;
            } else {
                //Debug.Log("Accessor: SetProperty: conditional accessor ignored null targetType");
                return false;
            }
        }

        if (accessor.excited) {
            
            string path = (string)jsonValue;
            //Debug.Log("Accessor: SetProperty: excited: jsonValue: " + jsonValue + " path: " + path);
            if (string.IsNullOrEmpty(path)) {

                value = null;

            } else {

                Accessor pathAccessor = null;
                if (!Accessor.FindAccessor(
                        target,
                        path,
                        ref pathAccessor)) {

                    Debug.LogError("Accessor: SetProperty: excited: can't find path accessor for target: " + target + " path: " + path);
                    return false;

                } else {

                    //Debug.Log("Accessor: SetProperty: excited: getting from pathAccessor: " + pathAccessor);
                    if (!pathAccessor.Get(ref value)) {

                        //Debug.Log("Accessor: SetProperty: excited: failed to get from pathAccessor: " + pathAccessor + " target: " + target + " path: " + path);

                        if (!pathAccessor.conditional) {
                            Debug.LogError("Accessor: SetProperty: excited: can't get pathAccessor: " + pathAccessor + " target: " + target + " path: " + path);
                            return false;
                        }

                        value = null;

                    } else {

                        //Debug.Log("Accessor: SetProperty: excited: got from pathAccessor: " + pathAccessor + " target: " + target + " path: " + path + " value: " + value);

                    }

                }

            }

        } else {

            if (!Bridge.mainBridge.ConvertToType(jsonValue, targetType, ref value)) {
                if (!accessor.conditional) {
                    Debug.LogError("Accessor: SetProperty: can't convert jsonValue: " + jsonValue + " to targetType: " + targetType);
                    return false;
                } else {
                    //Debug.Log("Accessor: SetProperty: conditional accessor could not convert type jsonValue: " + jsonValue + " to targetType: " + targetType + " target: " + target + " name: " + name);
                    return true;
                }
            }

        }

        //Debug.Log("Accessor: SetProperty: value: " + value);

        if (!accessor.Set(value)) {
            if (!accessor.conditional) {
                Debug.LogError("Accessor: SetProperty: can't set with accessor: " + accessor + " value: " + value);
                return false;
            } else {
                //Debug.Log("Accessor: SetProperty: not setting with conditional accessor: " + accessor + " value: " + value + " target: " + target + " name " + name);
                return true;
            }
        }

        //Debug.Log("Accessor: SetProperty: target: " + target + " name: " + name + " jsonValue: " + jsonValue + " targetType: " + targetType + " value: " + value + " accessor: " + accessor);

        return true;
    }


    public static bool GetPath(object target, string path, out object result)
    {
        result = null;

        Accessor accessor = null;
        if (!FindAccessor(
                target,
                path,
                ref accessor)) {

            Debug.LogError("Accessor: GetPath: can't find accessor for target: " + target + " path: " + path);
            return false;

        }

        if (!accessor.Get(ref result)) {

            if (!accessor.conditional) {
                Debug.LogError("Accessor: GetPath: can't get accessor: " + accessor + " target: " + target + " path: " + path);
                return false;
            }

        }

        return true;
    }


    public static bool SetPath(object target, string path, object value)
    {
        Accessor accessor = null;
        if (!FindAccessor(
                target,
                path,
                ref accessor)) {

            Debug.LogError("Accessor: SetPath: can't find accessor for target: " + target + " path: " + path);
            return false;

        }

        if (!accessor.Set(value)) {

            Debug.LogError("Accessor: SetPath: can't set accessor: " + accessor + " target: " + target + " path: " + path);
            return false;

        }

        return true;
    }


    public static System.Type FindTypeInLoadedAssemblies(string typeName)
    {
        System.Type foundType = null;

        //Debug.Log("Accessor: FindTypeInLoadedAssemblies: typeName: " + typeName + " assemblies: " + System.AppDomain.CurrentDomain.GetAssemblies());

        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {

            foundType = assembly.GetType(typeName);

            //Debug.Log("Accessor: FindTypeInLoadedAssemblies: typeName: " + typeName + " assembly: " + assembly + " foundType: " + foundType);

            if (foundType != null) {

                //Debug.Log("Accessor: FindTypeInLoadedAssemblies: typeName: " + typeName + " FOUND!!!!!!");

#if false
                foreach (PropertyInfo p in foundType.GetProperties()) {
                    Debug.Log("Accessor: FindTypeInLoadedAssemblies: typeName: " + typeName + " with property: " + p.Name);
                }
#endif

                break;
            }

        }

        return foundType;
    }


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    public void Clear()
    {
        type = AccessorType.Undefined;
        conditional = false;
        excited = false;
        index = 0;
        str = null;
        obj = null;
        objArray = null;
        objList = null;
        objDict = null;
        fieldInfo = null;
        propertyInfo = null;
    }


    public void Init_Constant(object obj0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.Constant;
        obj = obj0;
        conditional = conditional0;
        excited = excited0;
    }


    public void Init_JArray(JArray jarray0, int index0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.JArray;
        obj = jarray0;
        index = index0;
        conditional = conditional0;
        excited = excited0;
    }


    public void Init_Array(object[] objArray0, int index0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.Array;
        objArray = objArray0;
        index = index0;
        conditional = conditional0;
        excited = excited0;
    }


    public void Init_List(List<object> objList0, int index0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.List;
        objList = objList0;
        index = index0;
        conditional = conditional0;
        excited = excited0;
    }


    public void Init_JObject(JObject jobject0, string key0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.Dictionary;
        obj = jobject0;
        str = key0;
        conditional = conditional0;
        excited = excited0;
    }


    public void Init_Dictionary(Dictionary<string, object> objDict0, string key0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.Dictionary;
        objDict = objDict0;
        str = key0;
        conditional = conditional0;
        excited = excited0;
    }


    public void Init_Field(object obj0, string name0, FieldInfo fieldInfo0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.Field;
        obj = obj0;
        str = name0;
        fieldInfo = fieldInfo0;
        conditional = conditional0;
        excited = excited0;
    }
    

    public void Init_Property(object obj0, string name0, PropertyInfo propertyInfo0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.Property;
        obj = obj0;
        str = name0;
        propertyInfo = propertyInfo0;
        conditional = conditional0;
        excited = excited0;
    }


    public void Init_Transform(object xform0, string str0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.Transform;
        obj = xform0;
        str = str0;
        conditional = conditional0;
        excited = excited0;
    }


    public void Init_Component(object component0, string className0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.Component;
        obj = component0;
        str = className0;
        conditional = conditional0;
        excited = excited0;
    }


    public void Init_Resource(string resourcePath0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.Resource;
        str = resourcePath0;
        conditional = conditional0;
        excited = excited0;
    }


    public void Init_Object(string objectID0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.Object;
        str = objectID0;
        conditional = conditional0;
        excited = excited0;
    }


    public void Init_Method(object obj0, string methodName0, bool conditional0, bool excited0)
    {
        Clear();
        type = AccessorType.Method;
        obj = obj0;
        str = methodName0;
        conditional = conditional0;
        excited = excited0;
    }


    public bool CanGet()
    {
        switch (type) {

            case AccessorType.Undefined:
                return false;

            case AccessorType.Constant:
                return true;

            case AccessorType.JArray:
                return (index > 0) && (index < ((JArray)obj).Count);

            case AccessorType.Array:
                return (index > 0) && (index < objArray.Length);

            case AccessorType.List:
                return (index > 0) && (index < objList.Count);

            case AccessorType.JObject:
                return ((JObject)obj)[str] != null;

            case AccessorType.Dictionary:
                return objDict.ContainsKey(str);

            case AccessorType.Field:
                return true;

            case AccessorType.Property:
                return true;

            case AccessorType.Transform:
                return true;

            case AccessorType.Component:
                return true;

            case AccessorType.Resource:
                return true;

            case AccessorType.Object:
                return true;

            case AccessorType.Method:
                return true;

        }

        return false;
    }


    public bool Get(ref object result)
    {
        result = null;

        switch (type) {

            case AccessorType.Undefined:
                Debug.LogError("Accessor: Get: type: Unknown: error getting undefined accessor type");
                return false;

            case AccessorType.Constant:
                return Get_Constant(ref result);

            case AccessorType.JArray:
                return Get_JArray(ref result);

            case AccessorType.Array:
                return Get_Array(ref result);

            case AccessorType.List:
                return Get_List(ref result);

            case AccessorType.JObject:
                return Get_JObject(ref result);

            case AccessorType.Dictionary:
                return Get_Dictionary(ref result);

            case AccessorType.Field:
                return Get_Field(ref result);

            case AccessorType.Property:
                return Get_Property(ref result);

            case AccessorType.Transform:
                return Get_Transform(ref result);

            case AccessorType.Component:
                return Get_Component(ref result);

            case AccessorType.Resource:
                return Get_Resource(ref result);

            case AccessorType.Object:
                return Get_Object(ref result);

            case AccessorType.Method:
                return Get_Method(ref result);

        }

        return false;
    }


    public bool Get_Constant(ref object result)
    {
        result = obj;
        return true;
    }


    public bool Get_JArray(ref object result)
    {
        JArray jarray = (JArray)obj;
        //Debug.Log("Accessor: Get_JArray: index: " + index + " length: " + jarray.Count);
        if ((index >= 0) && 
            (index < jarray.Count)) {
            result = jarray[index];
            //Debug.Log("Accessor: Get_JArray: result: " + result);
            return true;
        } else {
            Debug.LogError("Accessor: Get_JArray: invalid index: " + index + " jarray: " + jarray);
            return false;
        }
    }


    public bool Get_Array(ref object result)
    {
        //Debug.Log("Accessor: Get_Array: index: " + index + " length: " + objArray.Length + " elementType: " + objArray.GetType().GetElementType());
        if ((index >= 0) && 
            (index < objArray.Length)) {
            result = objArray[index];
            //Debug.Log("Accessor: Get_Array: result: " + result);
            return true;
        } else {
            Debug.LogError("Accessor: Get_Array: invalid index: " + index + " objArray: " + objArray);
            return false;
        }
    }


    public bool Get_List(ref object result)
    {
        if ((index > 0) &&
            (index < objList.Count)) {
            result = objList[index];
            //Debug.Log("Accessor: Get_List: result: " + result);
            return true;
        } else {
            Debug.LogError("Accessor: Get_List: invalid index: " + index + " objList: " + objList);
            return false;
        }
    }


    public bool Get_JObject(ref object result)
    {
        JObject jobject = (JObject)obj;
        JToken token = jobject[str];
        if (token != null) {
            result = token;
            return true;
        } else {
            Debug.LogError("Accessor: Get_JObject: undefined str: " + str + " jobject: " + jobject);
            return false;
        }
    }


    public bool Get_Dictionary(ref object result)
    {
        if (objDict.ContainsKey(str)) {
            result = objDict[str];
            return true;
        } else {
            Debug.LogError("Accessor: Get_Dictionary: undefined str: " + str + " objDict: " + objDict);
            return false;
        }
    }


    public bool Get_Field(ref object result)
    {
        try {
            result = fieldInfo.GetValue(obj);
            return true;
        } catch (Exception ex) {
            Debug.LogError("Accessor: Get_Field: error getting value via fieldInfo: " + fieldInfo + " from obj: " + obj + " ex: " + ex);
            return false;
        }
    }


    public bool Get_Property(ref object result)
    {
        try {
#if UNITY_IOS
            // iOS AOT compiler requires this slower invocation.
            result = propertyInfo.GetGetMethod().Invoke(obj, null);
#else
            result = propertyInfo.GetValue(obj, null);
#endif
            return true;
        } catch (Exception ex) {
            Debug.LogError("Accessor: Get_Property: error getting value via propetyInfo: " + propertyInfo + " from obj: " + obj + " ex: " + ex);
            return false;
        }
    }


    public bool Get_Transform(ref object result)
    {
        Component component = obj as Component;
        GameObject gameObject = obj as GameObject;

        if ((component == null) &&
            (gameObject == null)) {
            Debug.LogError("Accessor: Get_Transform: not a Component or GameObject! obj: " + obj);
            return false;
        }

        if (component != null) {
            gameObject = component.gameObject;
        }

        Transform gameObjectTransform = gameObject.transform;

        switch (str) {

            case ".":
                result = gameObjectTransform;
                return true;

            case "..":
                result = gameObjectTransform.parent;
                return true;

            default:
                int index = 0;
                bool isInteger = int.TryParse(str, out index);

                if (isInteger) {

                    result =
                        ((index >= 0) &&
                         (index < gameObjectTransform.childCount))
                            ? gameObjectTransform.GetChild(index)
                            : null;
                    //Debug.Log("Accessor: Get_Transform gameObjectTransform: " + gameObjectTransform + " index: " + index + " result: " + result);
                    return true;


                } else {

                    result = 
                        gameObjectTransform.Find(str);
                    //Debug.Log("Accessor: Get_Transform gameObjectTransform: " + gameObjectTransform + " str: " + str + " result: " + result);
                    return true;

                }

        }
    }


    public bool Get_Component(ref object result)
    {
        Component component = obj as Component;
        GameObject go = 
            (component == null)
                ? (obj as GameObject)
                : component.gameObject;

        if (go == null) {
            Debug.LogError("Accessor: Get_Component: obj was not a Component or GameObject. obj: " + obj);
            return false;
        }

        //Debug.Log("Accessor: Get_Component obj: " + obj + " component: " + component + " go: " + go);

        System.Type componentType = FindTypeInLoadedAssemblies(str);

        //Debug.Log("Accessor: Get_Component FindTypeInLoadedAssemblies str: " + str + " componentType: " + componentType);

        if (componentType == null) {
            componentType = FindTypeInLoadedAssemblies("UnityJS." + str);
            //Debug.Log("Accessor: Get_Component FindTypeInLoadedAssemblies str: UnityJS." + str + " componentType: " + componentType);
        }

        if (componentType == null) {
            componentType = FindTypeInLoadedAssemblies("UnityEngine." + str);
            //Debug.Log("Accessor: Get_Component FindTypeInLoadedAssemblies str: UnityEngine." + str + " componentType: " + componentType);
        }

        if (componentType == null) {
            Debug.LogError("Accessor: Get_Component: can't get componentType! obj: " + obj + " str: " + str);
            return false;
        }

        Component otherComponent = go.GetComponent(componentType);

        //Debug.Log("Accessor: Get_Component: component: " + component + " Type: " + ((otherComponent == null) ? "null" : ("" + otherComponent.GetType())));

        if ((otherComponent == null) || otherComponent.Equals(null)) {
            return false;
        }

        result = otherComponent;
        return true;
    }


    public bool Get_Resource(ref object result)
    {
        UnityEngine.Object resource = 
            Resources.Load(str);

        if (resource != null) {
            result = resource;
            //Debug.Log("Accessor: Get_Resource: found str: " + str + " result: " + result);
            return true;
        } else {
            Debug.LogError("Accessor: Get_Resource: undefined str: " + str);
            return false;
        }
    }


    public bool Get_Object(ref object result)
    {
        object obj = 
            Bridge.mainBridge.GetObject(str);

        if (obj != null) {
            result = obj;
            //Debug.Log("Accessor: Get_Object: found str: " + str + " result: " + result);

            return true;
        } else {
            Debug.LogError("Accessor: Get_Object: undefined str: " + str);

            return false;
        }
    }


    public bool Get_Method(ref object result)
    {
        var type = 
            obj.GetType();
        //Debug.Log("Accessor: Get_Method: type: " + type);

        MethodInfo methodInfo = 
            type.GetMethod(str, Type.EmptyTypes);

        //Debug.Log("Accessor: Get_Method: 1 methodInfo: " + ((methodInfo == null) ? "NULL" : ("" + methodInfo)));

        List<object> parameters = new List<object>();

        // If we didn't find the method on the type, then look for an extension method.
        if (methodInfo == null) {

            methodInfo =
                type.GetExtensionMethod(str);

            // Extension methods take an additional obj parameter.
            if (methodInfo != null) {
                parameters.Add(obj);
            }

            //Debug.Log("Accessor: Get_Method: 2 methodInfo: " + ((methodInfo == null) ? "NULL" : ("" + methodInfo)));
        }

        if (methodInfo == null) {
            Debug.LogError("Accessor: Get_Method: can't find str: " + str + " on type: " + type);
            return false;
        }

        result = 
            methodInfo.Invoke(obj, parameters.ToArray());

        //Debug.Log("Accessor: Get_Method: Invoked str: " + str + " on obj: " + obj + "  type: " + ((result == null) ? "NULL" : result.GetType().Name) + " result: " + result);

        return true;
    }


    public bool CanSet()
    {
        switch (type) {

            case AccessorType.Undefined:
                return false;

            case AccessorType.Constant:
                return false;

            case AccessorType.JArray:
                return (index >= 0) && (index < ((JArray)obj).Count);

            case AccessorType.Array:
                return (index >= 0) && (index < objArray.Length);

            case AccessorType.List:
                return (index >= 0) && (index < objList.Count);

            case AccessorType.JObject:
                return true;

            case AccessorType.Dictionary:
                return true;

            case AccessorType.Field:
                return true;

            case AccessorType.Property:
                return true;

            case AccessorType.Transform:
                return false;

            case AccessorType.Component:
                return false;

            case AccessorType.Resource:
                return false;

            case AccessorType.Object:
                return false;

            case AccessorType.Method:
                return true;

        }

        return false;
    }


    public bool Set(object value)
    {
        switch (type) {

            case AccessorType.Undefined:
                Debug.LogError("Accessor: Get: type: Unknown: error setting undefined type");
                return false;

            case AccessorType.Constant:
                Debug.LogError("Accessor: Get: type: Unknown: error setting constant type");
                return false;

            case AccessorType.JArray:
                return Set_JArray(obj, value);

            case AccessorType.Array:
                return Set_Array(obj, value);

            case AccessorType.List:
                return Set_List(obj, value);

            case AccessorType.JObject:
                return Set_JObject(obj, value);

            case AccessorType.Dictionary:
                return Set_Dictionary(obj, value);

            case AccessorType.Field:
                return Set_Field(obj, value);

            case AccessorType.Property:
                return Set_Property(obj, value);

            case AccessorType.Transform:
                Debug.LogError("Accessor: Set: can't set type: Transform obj: " + obj);
                return false;

            case AccessorType.Component:
                Debug.LogError("Accessor: Set: can't set type: Component obj: " + obj);
                return false;

            case AccessorType.Resource:
                Debug.LogError("Accessor: Set: can't set type: Resource str: " + str);
                return false;

            case AccessorType.Object:
                Debug.LogError("Accessor: Set: can't set type: Object str: " + str);
                return false;

            case AccessorType.Method:
                return Set_Method(obj, value);

        }

        return false;
    }


    public bool Set_JArray(object obj, object value)
    {
        // TODO: Automatically convert various types to JSON.
        if ((value != null) &&
            (!(value is JToken))) {
            Debug.LogError("Accessor: Set_JArray: value not JToken or null.");
            return false;
        }

        JArray jarray = (JArray)obj;

        if ((index >= 0) && 
            (index < jarray.Count)) {
            // TODO: Automatically convert various types to JSON.
            JToken token = (JToken)obj;
            jarray[index] = token;
            return true;
        } else {
            Debug.LogError("Accessor: Set_Array: out of range array index: " + index + " Count: " + jarray.Count + " jarray: " + jarray);
            return false;
        }
    }


    public bool Set_Array(object obj, object value)
    {
        if ((index >= 0) && 
            (index < objArray.Length)) {
            objArray[index] = value;
            return true;
        } else {
            Debug.LogError("Accessor: Set_Array: out of range array index: " + index + " Length: " + objArray.Length + " objArray: " + objArray);
            return false;
        }
    }


    public bool Set_List(object obj, object value)
    {
        if ((index >= 0) && (index < objList.Count)) {
            objList[index] = value;
            return true;
        } else {
            Debug.LogError("Accessor: Set_List: out of range list index: " + index + " Count: " + objList.Count + " objList: " + objList);
            return false;
        }
    }


    public bool Set_JObject(object obj, object value)
    {
        // TODO: Automatically convert various types to JSON.
        if ((value != null) &&
            (!(value is JToken))) {
            Debug.LogError("Accessor: Set_JObject: value not JToken or null.");
            return false;
        }

        JObject jobject = (JObject)obj;
        JToken token = (JToken)value;

        jobject[str] = token;

        return true;
    }


    public bool Set_Dictionary(object obj, object value)
    {
        objDict[str] = value;
        return true;
    }


    public bool Set_Field(object obj, object value)
    {
        try {
            fieldInfo.SetValue(obj, value);
            return true;
        } catch (Exception ex) {
            Debug.LogError("Accessor: Set_Field: error setting value! obj: " + obj + " fieldInfo: " + fieldInfo + " ex: " + ex);
            return false;
        }
    }


    public bool Set_Property(object obj, object value)
    {
        try {

#if false
            propertyInfo.SetValue(obj, value, null);
#else
            MethodInfo setMethod = propertyInfo.GetSetMethod();
            if (setMethod == null) {
                Debug.LogError("Accessor: Set_Property: error setting value! Null setMethod for propertyInfo: " + propertyInfo + " obj: " + obj + " propertyInfo: " + propertyInfo);
                 return false;
            }
            setMethod.Invoke(obj, new object[] { value });
#endif

            return true;
        } catch (Exception ex) {
            Debug.LogError("Accessor: Set_Property: error setting value! obj: " + obj + " propertyInfo: " + propertyInfo + " ex: " + ex);
            return false;
        }
    }


    public bool Set_Method(object obj, object value)
    {
        var type = 
            obj.GetType();
        //Debug.Log("Accessor: Set_Method: type: " + type);

        MethodInfo methodInfo = 
            type.GetMethod(str, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        if (methodInfo == null) {
            methodInfo =
                type.GetExtensionMethod(str);
            //Debug.Log("Accessor: Set_Method: methodInfo: " + ((methodInfo == null) ? "NULL" : ("" + methodInfo)));
        }

        if (methodInfo == null) {
            Debug.LogError("Accessor: Set_Method: can't find str: " + str + " on type: " + type);
            return false;
        }

        ParameterInfo[] parameterInfos = methodInfo.GetParameters();
        //foreach (ParameterInfo parameterInfo in parameterInfos) {
            //Debug.Log("Accessor: Set_Method: parameterInfo: " + parameterInfo);
        //}

        List<object> parameters = new List<object>();

        if (methodInfo.IsStatic) {
            parameters.Add(obj);
        }

        JArray paramArray = value as JArray;
        if (paramArray == null) {
            Debug.LogError("Accessor: Set_Method: parameters should be an array: " + value);
            return false;
        }

        var staticStart = methodInfo.IsStatic ? 1 : 0;
        int expectedParams = parameterInfos.Length - staticStart;
        if (paramArray.Count != expectedParams) {
            Debug.LogError("Accessor: Set_Method: parameters should be an array of length " + expectedParams + ": " + value);
            return false;
        }

        for (int i = staticStart, n = parameterInfos.Length; i < n; i++) {
            ParameterInfo parameterInfo = parameterInfos[i];
            Type parameterType = parameterInfo.ParameterType;
            JToken jsParameter = paramArray[i - staticStart];
            object val = null;
            //Debug.Log("Accessor: Set_Method: about to convert to type jsParameter: " + jsParameter + " parameterType: " + parameterType);
            bool success = Bridge.mainBridge.ConvertToType(jsParameter, parameterType, ref val);
            if (!success) {
                Debug.LogError("Accessor: Set_Method: can't convert parameter to type: " + parameterType + " jsParameters: " + jsParameter + " parameterInfo: " + parameterInfo);
                return false;
            }
            //Debug.Log("Accessor: Set_Method: converted to val: " + val);
            parameters.Add(val);
        }

        //object result = 
        methodInfo.Invoke(obj, parameters.ToArray());

        //Debug.Log("Accessor: Set_Method: Invoked str: " + str + " on obj: " + obj + " result: " + result);

        return true;
    }
    

    public Type GetTargetType()
    {
        switch (type) {

            case AccessorType.Undefined:
                Debug.LogError("Accessor: Get: type: Unknown: error getting type of undefined accessor");
                return null;

            case AccessorType.Constant:
                if ((obj == null) || obj.Equals(null)) {
                    return null;
                }
                return obj.GetType();

            case AccessorType.JArray:
                return typeof(JToken);

            case AccessorType.Array:
                return objArray.GetType().GetElementType();

            case AccessorType.List:
                return objArray.GetType().GetGenericArguments()[0];

            case AccessorType.JObject:
                return typeof(JToken);

            case AccessorType.Dictionary:
                return objArray.GetType().GetGenericArguments()[1];

            case AccessorType.Field:
                return fieldInfo.FieldType;

            case AccessorType.Property:
                return propertyInfo.PropertyType;

            case AccessorType.Transform:
                return typeof(Transform);

            case AccessorType.Component:
                return typeof(Component);

            case AccessorType.Resource:
                return typeof(UnityEngine.Object);

            case AccessorType.Object:
                return typeof(object);

            case AccessorType.Method:
                return typeof(object);

        }

        return null;
    }
    

}


}
