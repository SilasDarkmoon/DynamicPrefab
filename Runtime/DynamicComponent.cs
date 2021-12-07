using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public class DynamicComponent : MonoBehaviour
    {
        public static readonly Dictionary<string, Func<DynamicComponent, Component>> CreatorFunctions = new Dictionary<string, Func<DynamicComponent, Component>>();

        [SerializeField]
        [HideInInspector]
        protected internal Component DynamicChild;

        public string ChildComponentName;
        public DataDictionary ChildComponentData;

        private void Awake()
        {
            var dcomps = gameObject.GetComponents<DynamicComponent>();
            if (dcomps != null)
            {
                for (int i = 0; i < dcomps.Length; ++i)
                {
                    var comp = dcomps[i];
                    comp.TryLoadDynamicChild();
                }
            }
        }

        public bool TryLoadDynamicChild()
        {
            if (!string.IsNullOrEmpty(ChildComponentName) && !DynamicChild)
            {
                Func<DynamicComponent, Component> func;
                if (CreatorFunctions.TryGetValue(ChildComponentName, out func))
                {
                    if (func != null)
                    {
                        DynamicChild = func(this);
                        return DynamicChild != null;
                    }
                }
            }
            return false;
        }

        //#if UNITY_EDITOR
        //        protected internal bool KeepDynamicChild;
        //        protected internal bool IsAsset;

        //        protected internal void DestroyDynamicChildInEditor()
        //        {
        //            if (!KeepDynamicChild)
        //            {
        //                if (IsAsset)
        //                {
        //                    if (DynamicChild)
        //                    {
        //                        DestroyImmediate(DynamicChild, true);
        //                        DynamicChild = null;
        //                    }
        //                }
        //            }
        //        }
        //#endif
    }
}