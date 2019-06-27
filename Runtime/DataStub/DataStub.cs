using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public abstract class DataStubBase : MonoBehaviour
    {
        public abstract object GetVal();
        internal abstract void SetVal(object val);
        public abstract Type GetValType();
    }
    public abstract class DataStub<T> : DataStubBase
    {
        [NonSerialized]
        public T Val;
        public override object GetVal()
        {
            return Val;
        }
        internal override void SetVal(object val)
        {
            //if (val == null)
            //{
            //    Val = default(T);
            //}
            //else
            if (val is T)
            {
                Val = (T)val;
            }
            else
            {
                Val = default(T);
            }
        }
        public override Type GetValType()
        {
            return typeof(T);
        }
    }

    public class DataStub : DataStub<object>
    {
    }

    public static class DataStubExtensions
    {
        public static T SafeGetVal<T>(this DataStub<T> stub)
        {
            if (stub)
            {
                return stub.Val;
            }
            return default(T);
        }
        public static T SafeGetVal<T>(this DataStub stub)
        {
            if (stub)
            {
                if (stub.Val is T)
                {
                    return (T)stub.Val;
                }
            }
            return default(T);
        }
        public static T SafeGetVal<T>(this DataStubObj stub) where T : Object
        {
            if (stub)
            {
                if (stub.Val is T)
                {
                    return (T)stub.Val;
                }
            }
            return default(T);
        }

        public static void SyncDynamicChildStubs(this GameObject go, IDictionary<string, object> dict)
        {
            if (go)
            {
                var par = go.transform;
                for (int i = 0; i < par.childCount; ++i)
                {
                    var child = par.GetChild(i);
                    var comp = child.GetComponent<DataStubBase>();
                    if (comp)
                    {
                        var name = child.name;
                        object val = null;
                        if (dict != null)
                        {
                            dict.TryGetValue(name, out val);
                        }
                        comp.SetVal(val);
                    }
                }
            }
        }
        public static void SyncDynamicChildStubs(this DynamicPrefab thiz)
        {
            if (thiz && thiz.DynamicChild)
            {
                IDictionary<string, object> childdata = null;
                var provider = thiz.DynamicChild.GetComponent<DataProviderBase>();
                if (provider)
                {
                    childdata = provider.GetData();
                }
                thiz.gameObject.SyncDynamicChildStubs(childdata);
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
    public static class DataStubComponentManager
    {
        private static Dictionary<Type, Type> _StubTypes = new Dictionary<Type, Type>();

        static DataStubComponentManager()
        {
            var monospts = UnityEditor.MonoImporter.GetAllRuntimeMonoScripts();
            if (monospts != null)
            {
                for (int i = 0; i < monospts.Length; ++i)
                {
                    var monospt = monospts[i];
                    if (monospt)
                    {
                        var type = monospt.GetClass();
                        if (type != null)
                        {
                            Type cType = type;
                            Type gType = null;
                            while (cType != null)
                            {
                                if (cType.IsGenericType && cType.GetGenericTypeDefinition() == typeof(DataStub<>))
                                {
                                    gType = cType;
                                    break;
                                }
                                cType = cType.BaseType;
                            }
                            if (gType != null)
                            {
                                var par = gType.GetGenericArguments()[0];
                                _StubTypes[par] = type;
                            }
                        }
                    }
                }
            }
        }

        public static Type GetStubComponentType(Type valType)
        {
            while (valType != null)
            {
                Type stubType;
                if (_StubTypes.TryGetValue(valType, out stubType))
                {
                    return stubType;
                }
                valType = valType.BaseType;
            }
            return null;
        }
        public static DataStubBase AddStubComponent(this GameObject go, object val)
        {
            if (go && val != null)
            {
                var type = val.GetType();
                Type stubtype = GetStubComponentType(type);
                DataStubBase comp;
                if (stubtype != null)
                {
                    comp = go.AddComponent(stubtype) as DataStubBase;
                }
                else
                {
                    if (type.IsSubclassOf(typeof(Object)))
                    {
                        comp = go.AddComponent<DataStubObj>();
                    }
                    else
                    {
                        comp = go.AddComponent<DataStub>();
                    }
                }
                if (comp)
                {
                    comp.SetVal(val);
                }
                return comp;
            }
            return null;
        }
        public static DataStubBase AddStubComponent<T>(this GameObject go, T val)
        {
            if (go)
            {
                var type = typeof(T);
                Type stubtype = GetStubComponentType(type);
                DataStubBase comp;
                if (stubtype != null)
                {
                    comp = go.AddComponent(stubtype) as DataStubBase;
                }
                else
                {
                    if (type.IsSubclassOf(typeof(Object)))
                    {
                        comp = go.AddComponent<DataStubObj>();
                    }
                    else
                    {
                        comp = go.AddComponent<DataStub>();
                    }
                }
                if (comp)
                {
                    if (comp is DataStub<T>)
                    {
                        ((DataStub<T>)comp).Val = val;
                    }
                    else
                    {
                        comp.SetVal(val);
                    }
                }
                return comp;
            }
            return null;
        }

        public static bool CreateDynamicChildStubs(this GameObject go, IDictionary<string, object> dict)
        {
            if (go)
            {
                bool changed = false;
                Dictionary<string, DataStubBase> old = new Dictionary<string, DataStubBase>();
                var par = go.transform;
                for (int i = 0; i < par.childCount; ++i)
                {
                    var child = par.GetChild(i);
                    var comp = child.GetComponent<DataStubBase>();
                    if (comp)
                    {
                        old[child.name] = comp;
                    }
                }
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        var key = kvp.Key;
                        var val = kvp.Value;
                        DataStubBase oldstub;
                        if (old.TryGetValue(key, out oldstub))
                        {
                            old.Remove(key);
                            var oldt = oldstub.GetValType();
                            if (val == null || oldt.IsAssignableFrom(val.GetType()))
                            {
                                oldstub.SetVal(val);
                            }
                            else
                            {
                                Object.Destroy(oldstub.gameObject);
                                var child = new GameObject(key);
                                child.transform.SetParent(par, false);
                                child.AddStubComponent(val);
                                changed = true;
                            }
                        }
                        else
                        {
                            var child = new GameObject(key);
                            child.transform.SetParent(par, false);
                            child.AddStubComponent(val);
                            changed = true;
                        }
                    }
                }
                if (old.Count > 0)
                {
                    changed = true;
                    foreach (var kvp in old)
                    {
                        Object.Destroy(kvp.Value.gameObject);
                    }
                }
                return changed;
            }
            return false;
        }
        public static bool MergeDynamicChildStubs(this GameObject go, IDictionary<string, object> dict)
        {
            if (go)
            {
                Dictionary<string, DataStubBase> old = new Dictionary<string, DataStubBase>();
                var par = go.transform;
                for (int i = 0; i < par.childCount; ++i)
                {
                    var child = par.GetChild(i);
                    var comp = child.GetComponent<DataStubBase>();
                    if (comp)
                    {
                        old[child.name] = comp;
                    }
                }
                if (dict != null)
                {
                    bool changed = false;
                    foreach (var kvp in dict)
                    {
                        var key = kvp.Key;
                        var val = kvp.Value;
                        DataStubBase oldstub;
                        if (old.TryGetValue(key, out oldstub))
                        {
                            old.Remove(key);
                            var oldt = oldstub.GetValType();
                            if (val == null || oldt.IsAssignableFrom(val.GetType()))
                            {
                                oldstub.SetVal(val);
                            }
                            else
                            {
                                Object.Destroy(oldstub.gameObject);
                                var child = new GameObject(key);
                                child.transform.SetParent(par, false);
                                child.AddStubComponent(val);
                                changed = true;
                            }
                        }
                        else
                        {
                            var child = new GameObject(key);
                            child.transform.SetParent(par, false);
                            child.AddStubComponent(val);
                            changed = true;
                        }
                    }
                    return changed;
                }
            }
            return false;
        }
        public static bool CreateDynamicChildStubs(this DynamicPrefab thiz)
        {
            if (thiz)
            {
                IDictionary<string, object> childdata = null;
                if (thiz.DynamicChild)
                {
                    var provider = thiz.DynamicChild.GetComponent<DataProviderBase>();
                    if (provider)
                    {
                        childdata = provider.GetData();
                    }
                }
                return thiz.gameObject.CreateDynamicChildStubs(childdata);
            }
            return false;
        }
        public static bool MergeDynamicChildStubs(this DynamicPrefab thiz)
        {
            if (thiz && thiz.DynamicChild)
            {
                IDictionary<string, object> childdata = null;
                var provider = thiz.DynamicChild.GetComponent<DataProviderBase>();
                if (provider)
                {
                    childdata = provider.GetData();
                }
                return thiz.gameObject.MergeDynamicChildStubs(childdata);
            }
            return false;
        }
    }
#endif
}