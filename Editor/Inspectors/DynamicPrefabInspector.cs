using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;

using Object = UnityEngine.Object;

namespace Capstones.UnityEditorEx
{
    [InitializeOnLoad]
    [CustomEditor(typeof(DynamicPrefab))]
    public class DynamicPrefabInspector : InspectorBase<DynamicPrefab>
    {
        #region SerializedObject and Property
        private SerializedObject soTarget = null;
        private SerializedProperty spSource;
        private SerializedProperty spEvent;
        private string oldSource;
        private Object oldPrefab;
        private bool justEnabled;
        #endregion

        void OnEnable()
        {
            soTarget = new SerializedObject(target);
            spSource = soTarget.FindProperty("Source");
            spEvent = soTarget.FindProperty("OnDynamicChildLoaded");
            justEnabled = true;
        }

        public override void OnInspectorGUI()
        {
            bool prefabChanged = false;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sync Data"))
            {
                //stubButtonClicked = true;
                Target.SyncDynamicChildStubs();
            }
            if (GUILayout.Button("Merge Stub"))
            {
                prefabChanged = Target.MergeDynamicChildStubs();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Re-create Stub"))
            {
                prefabChanged = Target.CreateDynamicChildStubs();
            }
            EditorGUILayout.EndHorizontal();
            if (prefabChanged)
            {
                PrefabStageExtensions.TrySavePrefabStage();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(Target.gameObject.scene);
            }

            soTarget.Update();
            EditorGUILayout.PropertyField(spSource, new GUIContent("Source"));
            if (oldSource != spSource.stringValue)
            {
                oldSource = spSource.stringValue ?? "";
                oldPrefab = null;
                var path = ResManager.EditorResLoader.CheckDistributePath("CapsRes/" + oldSource, true);
                if (path != null)
                {
                    oldPrefab = AssetDatabase.LoadMainAssetAtPath(path);
                    if (!justEnabled)
                    {
                        Target.MergeStubFromPrefab(oldPrefab as GameObject);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(Target.gameObject.scene);
                    }
                }
            }
            justEnabled = false;
            Object oldObj = oldPrefab;
            var newObj = EditorGUILayout.ObjectField("Prefab", oldObj, typeof(GameObject), false);
            if (newObj != oldObj)
            {
                string newNorm = null;
                if (newObj != null)
                {
                    var newPath = AssetDatabase.GetAssetPath(newObj);
                    newNorm = CapsResInfoEditor.GetAssetNormPath(newPath);
                }
                newNorm = newNorm ?? "";
                spSource.stringValue = newNorm;
                oldPrefab = newObj;
                oldSource = newNorm;
            }
            EditorGUILayout.PropertyField(spEvent, new GUIContent("On Dynamic Child Loaded"));
            soTarget.ApplyModifiedProperties();
        }

        private static class NativeImported
        {
            [DllImport("gdi32")]
            public static extern uint GetPixel(IntPtr hDC, int XPos, int YPos);
            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;
                public POINT(int x, int y)
                {
                    X = x;
                    Y = y;
                }
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern bool GetCursorPos(out POINT pt);
        }

        public static Color GetColorAtScreenPos(Vector2 pos)
        {
            IntPtr dc = NativeImported.GetWindowDC(IntPtr.Zero);
            uint colorn = NativeImported.GetPixel(dc, (int)pos.x, (int)pos.y);

            byte a = 255; //(byte)((colorn & (0xFF << 24)) >> 24);
            byte b = (byte)((colorn & (0xFF << 16)) >> 16);
            byte g = (byte)((colorn & (0xFF << 8)) >> 8);
            byte r = (byte)(colorn & 0xFF);

            return new Color32(r, g, b, a);
        }
        public static Color GetColorAtGUIPos(Vector2 pos)
        {
            pos = EditorGUIUtility.GUIToScreenPoint(pos);
            //pos.x = Screen.height - pos.x;
            return GetColorAtScreenPos(pos);
        }
        private static Color GetDefaultBackgroundColor()
        {
            float kViewBackgroundIntensity = EditorGUIUtility.isProSkin ? 0.22f : 0.76f;
            return new Color(kViewBackgroundIntensity, kViewBackgroundIntensity, kViewBackgroundIntensity, 1f);
        }

        static DynamicPrefabInspector()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }
        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            //if (Event.current.type == EventType.Repaint)
            {
                var obj = EditorUtility.InstanceIDToObject(instanceID);
                if (obj != null)
                {
                    if (obj is GameObject)
                    {
                        var go = obj as GameObject;
                        bool shouldredraw = false;
                        Color fontColor = Color.green;
                        var dprefab = go.GetComponent<DynamicPrefab>();
                        if (dprefab != null)
                        {
                            shouldredraw = true;
                            fontColor = Color.green;
                            Color prefabColor = Color.green;

                            var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
                            if (prefabStatus == PrefabInstanceStatus.Connected)
                            {
                                prefabColor = Color.blue;
                            }
                            else if (prefabStatus == PrefabInstanceStatus.MissingAsset)
                            {
                                prefabColor = Color.red;
                            }
                            fontColor = (fontColor + prefabColor);// / 2;
                            fontColor.r = Mathf.Clamp01(fontColor.r);
                            fontColor.g = Mathf.Clamp01(fontColor.g);
                            fontColor.b = Mathf.Clamp01(fontColor.b);
                            fontColor.a = Mathf.Clamp01(fontColor.a);
                        }
                        else
                        {
                            var stub = go.GetComponent<DataStubBase>();
                            if (stub)
                            {
                                if (!IsDynamicChild(go.transform))
                                {
                                    shouldredraw = true;
                                    fontColor = Color.yellow;

                                    //var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
                                    //if (prefabStatus == PrefabInstanceStatus.Connected)
                                    //{
                                    //    fontColor.a = 0.5f;
                                    //}
                                    //else if (prefabStatus == PrefabInstanceStatus.MissingAsset)
                                    //{
                                    //    fontColor.a = 0.5f;
                                    //}
                                }
                            }
                        }
                        if (!EditorGUIUtility.isProSkin)
                        {
                            fontColor = (fontColor + Color.black) / 2;
                            if (go.activeInHierarchy)
                            {
                                fontColor = (fontColor + Color.black) / 2;
                            }
                        }
                        else
                        {
                            if (!go.activeInHierarchy)
                            {
                                fontColor = (fontColor + Color.black) / 2;
                            }
                        }
                        if (shouldredraw)
                        { 
                            var offset = EditorStyles.toggle.CalcSize(GUIContent.none).x;
                            var labelWidth = EditorStyles.label.CalcSize(new GUIContent(obj.name)).x;
                            labelWidth = Math.Min(labelWidth, selectionRect.size.x - offset);
                            var vspace = EditorGUIUtility.standardVerticalSpacing;
                            Rect offsetRect = new Rect(selectionRect.position + new Vector2(offset, vspace), new Vector2(labelWidth, selectionRect.size.y - vspace));

                            ////Draw Background
                            //var skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
                            //var backColor = GetDefaultBackgroundColor();
                            //if (Selection.Contains(instanceID))
                            //{
                            //    var focusWin = EditorWindow.focusedWindow;
                            //    if (focusWin && focusWin.GetType().Name == "SceneHierarchyWindow")
                            //    {
                            //        var scolor = skin.settings.selectionColor;
                            //        var alpha = scolor.a * 0.85f;
                            //        backColor = backColor * (1.0f - alpha) + scolor * alpha;
                            //        backColor.a = 1;
                            //    }
                            //    else
                            //    {
                            //        backColor *= 1.3f;
                            //        backColor.a = 1.0f;
                            //    }
                            //}
                            //else
                            //{
                            //    var checkRect = selectionRect;
                            //    var mouseWin = EditorWindow.mouseOverWindow;
                            //    if (mouseWin && mouseWin.GetType().Name == "SceneHierarchyWindow")
                            //    {
                            //        checkRect.xMin = 0;
                            //        checkRect.width = mouseWin.position.width;
                            //    }
                            //    if (checkRect.Contains(Event.current.mousePosition))
                            //    {
                            //        backColor *= 0.85f;
                            //        backColor.a = 1.0f;
                            //    }
                            //}
                            //EditorGUI.DrawRect(offsetRect, backColor);

                            // Draw name label
                            EditorGUI.LabelField(offsetRect, obj.name, new GUIStyle()
                            {
                                normal = new GUIStyleState() { textColor = fontColor },
                            });
                        }
                    }
                }
            }
        }

        public static bool IsDynamicChild(Transform trans)
        {
            while (trans)
            {
                var parent = trans.parent;
                if (parent)
                {
                    var par = parent.GetComponent<DynamicPrefab>();
                    if (par)
                    {
                        if (par.DynamicChild == trans.gameObject)
                        {
                            return true;
                        }
                    }
                }
                trans = parent;
            }
            return false;
        }
    }

    public static class PrefabStageExtensions
    {
        private static System.Reflection.MethodInfo _mIsMainStage;
        public static System.Reflection.MethodInfo mIsMainStage
        {
            get
            {
                if (_mIsMainStage == null)
                {
                    try
                    {
                        _mIsMainStage = typeof(UnityEditor.SceneManagement.StageHandle).GetProperty("isMainStage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetMethod;
                    }
                    catch { }
                }
                return _mIsMainStage;
            }
        }
        private static Func<UnityEditor.SceneManagement.StageHandle, bool> _FuncIsMainStage;
        public static Func<UnityEditor.SceneManagement.StageHandle, bool> FuncIsMainStage
        {
            get
            {
                if (_FuncIsMainStage == null)
                {
                    var parHandle = System.Linq.Expressions.Expression.Parameter(typeof(UnityEditor.SceneManagement.StageHandle), "handle");
                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<UnityEditor.SceneManagement.StageHandle, bool>>(System.Linq.Expressions.Expression.Call(parHandle, mIsMainStage), parHandle);
                    _FuncIsMainStage = lambda.Compile();
                }
                return _FuncIsMainStage;
            }
        }
        public static bool IsMainStage(this UnityEditor.SceneManagement.StageHandle handle)
        {
            return FuncIsMainStage(handle);
        }

        private static Type _tStageNavigationManager;
        public static Type tStageNavigationManager
        {
            get
            {
                if (_tStageNavigationManager == null)
                {
                    try
                    {
                        _tStageNavigationManager = typeof(UnityEditor.SceneManagement.EditorSceneManager).Assembly.GetType("UnityEditor.SceneManagement.StageNavigationManager");
                    }
                    catch { }
                }
                return _tStageNavigationManager;
            }
        }
        private static object _iStageNavigationManager;
        public static object iStageNavigationManager
        {
            get
            {
                if (_iStageNavigationManager == null)
                {
                    _iStageNavigationManager = tStageNavigationManager.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
                }
                return _iStageNavigationManager;
            }
        }
        private static System.Reflection.MethodInfo _mGetCurrentPrefabStage;
        public static System.Reflection.MethodInfo mGetCurrentPrefabStage
        {
            get
            {
                if (_mGetCurrentPrefabStage == null)
                {
                    _mGetCurrentPrefabStage = tStageNavigationManager.GetMethod("GetCurrentPrefabStage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                }
                return _mGetCurrentPrefabStage;
            }
        }
        private static Func<object, object> _funcGetCurrentPrefabStage;
        public static Func<object, object> funcGetCurrentPrefabStage
        {
            get
            {
                if (_funcGetCurrentPrefabStage == null)
                {
                    var parThis = System.Linq.Expressions.Expression.Parameter(typeof(object), "thiz");
                    var exConvert = System.Linq.Expressions.Expression.Convert(parThis, tStageNavigationManager);
                    _funcGetCurrentPrefabStage = System.Linq.Expressions.Expression.Lambda<Func<object, object>>(System.Linq.Expressions.Expression.Call(exConvert, mGetCurrentPrefabStage), parThis).Compile();
                }
                return _funcGetCurrentPrefabStage;
            }
        }
        private static Type _tPrefabStage;
        public static Type tPrefabStage
        {
            get
            {
                if (_tPrefabStage == null)
                {
                    _tPrefabStage = mGetCurrentPrefabStage.ReturnType;
                }
                return _tPrefabStage;
            }
        }
        private static System.Reflection.PropertyInfo _pprefabAssetPath;
        public static System.Reflection.PropertyInfo pprefabAssetPath
        {
            get
            {
                if (_pprefabAssetPath == null)
                {
                    _pprefabAssetPath = tPrefabStage.GetProperty("prefabAssetPath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                }
                return _pprefabAssetPath;
            }
        }
        private static Func<object, string> _funcGetPrefabAssetPath;
        public static Func<object, string> funcGetPrefabAssetPath
        {
            get
            {
                if (_funcGetPrefabAssetPath == null)
                {
                    var parThis = System.Linq.Expressions.Expression.Parameter(typeof(object), "thiz");
                    var exConvert = System.Linq.Expressions.Expression.Convert(parThis, tPrefabStage);
                    _funcGetPrefabAssetPath = System.Linq.Expressions.Expression.Lambda<Func<object, string>>(System.Linq.Expressions.Expression.Property(exConvert, pprefabAssetPath), parThis).Compile();
                }
                return _funcGetPrefabAssetPath;
            }
        }
        private static System.Reflection.PropertyInfo _pprefabContentsRoot;
        public static System.Reflection.PropertyInfo pprefabContentsRoot
        {
            get
            {
                if (_pprefabContentsRoot == null)
                {
                    _pprefabContentsRoot = tPrefabStage.GetProperty("prefabContentsRoot", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                }
                return _pprefabContentsRoot;
            }
        }
        private static Func<object, GameObject> _funcGetPrefabContentsRoot;
        public static Func<object, GameObject> funcGetPrefabContentsRoot
        {
            get
            {
                if (_funcGetPrefabContentsRoot == null)
                {
                    var parThis = System.Linq.Expressions.Expression.Parameter(typeof(object), "thiz");
                    var exConvert = System.Linq.Expressions.Expression.Convert(parThis, tPrefabStage);
                    _funcGetPrefabContentsRoot = System.Linq.Expressions.Expression.Lambda<Func<object, GameObject>>(System.Linq.Expressions.Expression.Property(exConvert, pprefabContentsRoot), parThis).Compile();
                }
                return _funcGetPrefabContentsRoot;
            }
        }

        public static object GetCurrentPrefabStage()
        {
            return funcGetCurrentPrefabStage(iStageNavigationManager);
        }
        public static string GetPrefabAssetPath(object stage)
        {
            return funcGetPrefabAssetPath(stage);
        }
        public static GameObject GetPrefabContentsRoot(object stage)
        {
            return funcGetPrefabContentsRoot(stage);
        }
        public static void TrySavePrefabStage()
        {
            var stage = GetCurrentPrefabStage();
            if (stage != null)
            {
                PrefabUtility.SaveAsPrefabAsset(GetPrefabContentsRoot(stage), GetPrefabAssetPath(stage));
            }
        }
    }
}