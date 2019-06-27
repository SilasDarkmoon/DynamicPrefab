using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;

using Object = UnityEngine.Object;

namespace Capstones.UnityEditorEx
{
    [CustomEditor(typeof(DataStubBase), true)]
    public class DataStubInspector : InspectorBase<DataStubBase>
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            object val = null;
            if (Target != null)
            {
                val = Target.GetVal();
            }
            if (ReferenceEquals(val, null) && Target != null && typeof(Object).IsAssignableFrom(Target.GetValType()))
            {
                EditorGUILayout.ObjectField("Val:", null, Target != null ? Target.GetValType() : typeof(Object), true);
            }
            else if (val is Object)
            {
                EditorGUILayout.ObjectField("Val:", val as Object, Target != null ? Target.GetValType() : val.GetType(), true);
            }
            else
            {
                EditorGUILayout.TextField("Val:", (val ?? "").ToString());
            }
            GUI.enabled = true;
        }
    }
}