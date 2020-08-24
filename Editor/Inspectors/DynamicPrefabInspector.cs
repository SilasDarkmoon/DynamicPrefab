using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
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
        //private static Color GetDefaultBackgroundColor()
        //{
        //    float kViewBackgroundIntensity = EditorGUIUtility.isProSkin ? 0.22f : 0.76f;
        //    return new Color(kViewBackgroundIntensity, kViewBackgroundIntensity, kViewBackgroundIntensity, 1f);
        //}
        private static Func<Color> _GetDefaultBackgroundColorFunc;
        private static Color GetDefaultBackgroundColor()
        {
            if (_GetDefaultBackgroundColorFunc == null)
            {
                _GetDefaultBackgroundColorFunc = (Func<Color>)Delegate.CreateDelegate(typeof(Func<Color>), typeof(EditorGUIUtility).GetMethod("GetDefaultBackgroundColor", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
            }
            return _GetDefaultBackgroundColorFunc();
        }
        private static Type _Type_HierarchyStyles;
        private static Type GetHierarchyStyles()
        {
            if (_Type_HierarchyStyles == null)
            {
                Type tTreeViewGUI = typeof(EditorGUI).Assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewGUI");
                _Type_HierarchyStyles = tTreeViewGUI.GetNestedType("Styles", System.Reflection.BindingFlags.NonPublic);
            }
            return _Type_HierarchyStyles;
        }
        private static Func<GUIStyle> _GetHierarchyStyle_Selection_Func;
        private static GUIStyle GetHierarchyStyle_Selection()
        {
            if (_GetHierarchyStyle_Selection_Func == null)
            {
                var fi = GetHierarchyStyles().GetField("selectionStyle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                _GetHierarchyStyle_Selection_Func = Expression.Lambda<Func<GUIStyle>>(Expression.Field(null, fi)).Compile();
            }
            return _GetHierarchyStyle_Selection_Func();
        }
        private static Type _Type_GameObjectStyles;
        private static Type GetGameObjectStyles()
        {
            if (_Type_GameObjectStyles == null)
            {
                Type tGameObjectTreeViewGUI = typeof(EditorGUI).Assembly.GetType("UnityEditor.GameObjectTreeViewGUI");
                _Type_GameObjectStyles = tGameObjectTreeViewGUI.GetNestedType("GameObjectStyles", System.Reflection.BindingFlags.NonPublic);
            }
            return _Type_GameObjectStyles;
        }
        private static Func<Color> _GetGameObjectStyle_HoveredBackgroundColor_Func;
        private static Color GetGameObjectStyle_HoveredBackgroundColor()
        {
            if (_GetGameObjectStyle_HoveredBackgroundColor_Func == null)
            {
                var fi = GetGameObjectStyles().GetField("hoveredBackgroundColor", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                _GetGameObjectStyle_HoveredBackgroundColor_Func = Expression.Lambda<Func<Color>>(Expression.Field(null, fi)).Compile();
            }
            return _GetGameObjectStyle_HoveredBackgroundColor_Func();
        }
        private static Func<GUIStyle> _GetGameObjectStyle_DisabledLabel_Func;
        private static GUIStyle GetGameObjectStyle_DisabledLabel()
        {
            if (_GetGameObjectStyle_DisabledLabel_Func == null)
            {
                var fi = GetGameObjectStyles().GetField("disabledLabel", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                _GetGameObjectStyle_DisabledLabel_Func = Expression.Lambda<Func<GUIStyle>>(Expression.Field(null, fi)).Compile();
            }
            return _GetGameObjectStyle_DisabledLabel_Func();
        }
        private static Type _Type_TreeViewStyles;
        private static Type GetTreeViewStyles()
        {
            if (_Type_TreeViewStyles == null)
            {
                Type tTreeViewGUI = typeof(EditorGUI).Assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewGUI");
                _Type_TreeViewStyles = tTreeViewGUI.GetNestedType("Styles", System.Reflection.BindingFlags.NonPublic);
            }
            return _Type_TreeViewStyles;
        }
        private static Func<GUIStyle> _GetTreeViewStyle_LineStyle_Func;
        private static GUIStyle GetTreeViewStyle_LineStyle()
        {
            if (_GetTreeViewStyle_LineStyle_Func == null)
            {
                var fi = GetTreeViewStyles().GetField("lineStyle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                _GetTreeViewStyle_LineStyle_Func = Expression.Lambda<Func<GUIStyle>>(Expression.Field(null, fi)).Compile();
            }
            return _GetTreeViewStyle_LineStyle_Func();
        }

        public abstract class NonPublicTypeWrapper<TRaw>
        {
            protected TRaw _Raw;
            public TRaw Raw { get { return _Raw; } }
        }
        public class SceneHierarchyWindowWrapper : NonPublicTypeWrapper<EditorWindow>
        {
            private SceneHierarchyWindowWrapper(EditorWindow win)
            {
                _Raw = win;
            }

            private static Type _Type_SceneHierarchyWindow;
            public static Type Type_SceneHierarchyWindow
            {
                get
                {
                    if (_Type_SceneHierarchyWindow == null)
                    {
                        _Type_SceneHierarchyWindow = typeof(EditorGUI).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                    }
                    return _Type_SceneHierarchyWindow;
                }
            }

            public static SceneHierarchyWindowWrapper[] GetAllSceneHierarchyWindows()
            {
                var wins = Resources.FindObjectsOfTypeAll(Type_SceneHierarchyWindow);
                SceneHierarchyWindowWrapper[] results = new SceneHierarchyWindowWrapper[wins.Length];
                for (int i = 0; i < wins.Length; ++i)
                {
                    results[i] = new SceneHierarchyWindowWrapper(wins[i] as EditorWindow);
                }
                return results;
            }
            public static SceneHierarchyWindowWrapper GetSceneHierarchyWindowAt(Rect rect)
            {
                var all = GetAllSceneHierarchyWindows();
                for (int i = 0; i < all.Length; ++i)
                {
                    var win = all[i];
                    if (win._Raw.position.Overlaps(rect))
                    {
                        return win;
                    }
                }
                return null;
            }

            private static Func<EditorWindow, object> _Getter_SceneHierarchy;
            public SceneHierarchyWrapper SceneHierarchy
            {
                get
                {
                    if (_Getter_SceneHierarchy == null)
                    {
                        var pi = Type_SceneHierarchyWindow.GetProperty("sceneHierarchy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        var par = Expression.Parameter(typeof(EditorWindow));
                        _Getter_SceneHierarchy =
                            Expression.Lambda<Func<EditorWindow, object>>(
                                Expression.Property(
                                    Expression.Convert(par, Type_SceneHierarchyWindow)
                                    , pi
                                    )
                                , par
                                ).Compile();
                    }
                    return new SceneHierarchyWrapper(_Getter_SceneHierarchy(_Raw));
                }
            }
        }
        public class SceneHierarchyWrapper : NonPublicTypeWrapper<object>
        {
            private static Type _Type_SceneHierarchy;
            public static Type Type_SceneHierarchy { get { return _Type_SceneHierarchy; } }

            internal SceneHierarchyWrapper(object raw)
            {
                _Raw = raw;
                if (_Type_SceneHierarchy == null)
                {
                    _Type_SceneHierarchy = raw.GetType();
                }
            }

            private static Func<object, object> _Getter_TreeView;
            public TreeViewControllerWrapper TreeView
            {
                get
                {
                    if (_Getter_TreeView == null)
                    {
                        var pi = Type_SceneHierarchy.GetProperty("treeView", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        var par = Expression.Parameter(typeof(object));
                        _Getter_TreeView = Expression.Lambda<Func<object, object>>(
                            Expression.Property(
                                Expression.Convert(par, Type_SceneHierarchy)
                                , pi
                                )
                            , par
                            ).Compile();
                    }
                    return new TreeViewControllerWrapper(_Getter_TreeView(_Raw));
                }
            }
        }
        public class TreeViewControllerWrapper : NonPublicTypeWrapper<object>
        {
            private static Type _Type_TreeViewController;
            public static Type Type_TreeViewController { get { return _Type_TreeViewController; } }

            internal TreeViewControllerWrapper(object raw)
            {
                _Raw = raw;
                if (_Type_TreeViewController == null)
                {
                    _Type_TreeViewController = raw.GetType();
                }
            }

            private static Func<object, object> _Getter_Data;
            public ITreeViewDataSourceWrapper Data
            {
                get
                {
                    if (_Getter_Data == null)
                    {
                        var pi = Type_TreeViewController.GetProperty("data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        var par = Expression.Parameter(typeof(object));
                        _Getter_Data = Expression.Lambda<Func<object, object>>(
                            Expression.Property(
                                Expression.Convert(par, Type_TreeViewController)
                                , pi
                                )
                            , par
                            ).Compile();
                    }
                    return new ITreeViewDataSourceWrapper(_Getter_Data(_Raw));
                }
            }

            private static Func<object, TreeViewItem, bool> _Func_IsItemDragSelectedOrSelected;
            public bool IsItemDragSelectedOrSelected(TreeViewItem item)
            {
                if (_Func_IsItemDragSelectedOrSelected == null)
                {
                    var mi = Type_TreeViewController.GetMethod("IsItemDragSelectedOrSelected", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var tar = Expression.Parameter(typeof(object));
                    var par = Expression.Parameter(typeof(TreeViewItem));
                    _Func_IsItemDragSelectedOrSelected = Expression.Lambda<Func<object, TreeViewItem, bool>>(
                        Expression.Call(
                            Expression.Convert(tar, Type_TreeViewController)
                            , mi, par
                            )
                        , tar, par
                        ).Compile();
                }
                return _Func_IsItemDragSelectedOrSelected(_Raw, item);
            }

            private static Func<object, TreeViewItem> _Getter_HoveredItem;
            public TreeViewItem HoveredItem
            {
                get
                {
                    if (_Getter_HoveredItem == null)
                    {
                        var pi = Type_TreeViewController.GetProperty("hoveredItem", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        var par = Expression.Parameter(typeof(object));
                        _Getter_HoveredItem = Expression.Lambda<Func<object, TreeViewItem>>(
                            Expression.Property(
                                Expression.Convert(par, Type_TreeViewController)
                                , pi
                                )
                            , par
                            ).Compile();
                    }
                    return _Getter_HoveredItem(_Raw);
                }
            }
        }
        public class ITreeViewDataSourceWrapper : NonPublicTypeWrapper<object>
        {
            private static Type _Type_ITreeViewDataSource;
            public static Type Type_ITreeViewDataSource
            {
                get
                {
                    if (_Type_ITreeViewDataSource == null)
                    {
                        _Type_ITreeViewDataSource = typeof(EditorGUI).Assembly.GetType("UnityEditor.IMGUI.Controls.ITreeViewDataSource");
                    }
                    return _Type_ITreeViewDataSource;
                }
            }

            internal ITreeViewDataSourceWrapper(object raw)
            {
                _Raw = raw;
            }

            private static Func<object, IList<TreeViewItem>> _Func_GetRows;
            public IList<TreeViewItem> GetRows()
            {
                if (_Func_GetRows == null)
                {
                    var mi = Type_ITreeViewDataSource.GetMethod("GetRows");
                    var par = Expression.Parameter(typeof(object));
                    _Func_GetRows = Expression.Lambda<Func<object, IList<TreeViewItem>>>(
                        Expression.Call(
                            Expression.Convert(par, Type_ITreeViewDataSource)
                            , mi)
                        , par
                        ).Compile();
                }
                return _Func_GetRows(_Raw);
            }

            private static Func<object, int, TreeViewItem> _Func_FindItem;
            public TreeViewItem FindItem(int id)
            {
                if (_Func_FindItem == null)
                {
                    var mi = Type_ITreeViewDataSource.GetMethod("FindItem");
                    var tar = Expression.Parameter(typeof(object));
                    var par = Expression.Parameter(typeof(int));
                    _Func_FindItem = Expression.Lambda<Func<object, int, TreeViewItem>>(
                        Expression.Call(
                            Expression.Convert(tar, Type_ITreeViewDataSource)
                            , mi, par)
                        , tar, par
                        ).Compile();
                }
                return _Func_FindItem(_Raw, id);
            }
        }

        public delegate void HierarchyWindowItemOnGUIExCallback(SceneHierarchyWindowWrapper win, TreeViewItem item, int instanceID, Rect rect);
        public static event HierarchyWindowItemOnGUIExCallback OnHandleHierarchyWindowItemOnGUIEx;
        public static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (OnHandleHierarchyWindowItemOnGUIEx != null)
            {
                SceneHierarchyWindowWrapper win = SceneHierarchyWindowWrapper.GetSceneHierarchyWindowAt(GUIUtility.GUIToScreenRect(selectionRect));
                var item = win.SceneHierarchy.TreeView.Data.FindItem(instanceID);
                OnHandleHierarchyWindowItemOnGUIEx(win, item, instanceID, selectionRect);
            }
        }

        static DynamicPrefabInspector()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
            OnHandleHierarchyWindowItemOnGUIEx += HandleHierarchyWindowItemOnGUIEx;
        }
        private static void HandleHierarchyWindowItemOnGUIEx(SceneHierarchyWindowWrapper win, TreeViewItem item, int instanceID, Rect selectionRect)
        {
            if (Event.current.type == EventType.Repaint)
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
                            //Color prefabColor = Color.green;

                            //var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
                            //if (prefabStatus == PrefabInstanceStatus.Connected)
                            //{
                            //    prefabColor = Color.blue;
                            //}
                            //else if (prefabStatus == PrefabInstanceStatus.MissingAsset)
                            //{
                            //    prefabColor = Color.red;
                            //}
                            //fontColor = (fontColor + prefabColor);// / 2;
                            //fontColor.r = Mathf.Clamp01(fontColor.r);
                            //fontColor.g = Mathf.Clamp01(fontColor.g);
                            //fontColor.b = Mathf.Clamp01(fontColor.b);
                            //fontColor.a = Mathf.Clamp01(fontColor.a);

                            var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
                            if (prefabStatus == PrefabInstanceStatus.Connected)
                            {
                                fontColor = Color.cyan;
                            }
                            else if (prefabStatus == PrefabInstanceStatus.MissingAsset)
                            {
                                fontColor = new Color(1f, 0.5f, 0f, 1f);
                            }
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
                        //if (!EditorGUIUtility.isProSkin)
                        //{
                        //    fontColor = (fontColor + Color.black) / 2;
                        //    if (go.activeInHierarchy)
                        //    {
                        //        fontColor = (fontColor + Color.black) / 2;
                        //    }
                        //}
                        //else
                        //{
                        //    if (!go.activeInHierarchy)
                        //    {
                        //        fontColor = (fontColor + Color.black) / 2;
                        //    }
                        //}
                        if (shouldredraw)
                        { 
                            var offset = EditorStyles.toggle.CalcSize(GUIContent.none).x;
                            var labelWidth = EditorStyles.label.CalcSize(new GUIContent(obj.name)).x;
                            labelWidth = Math.Min(labelWidth, selectionRect.size.x - offset);
                            //var vspace = EditorGUIUtility.standardVerticalSpacing;
                            var vspace = 0;
                            Rect offsetRect = new Rect(selectionRect.position + new Vector2(offset, vspace), new Vector2(labelWidth, selectionRect.size.y - vspace));

                            // Color of name Label
                            Color defaultLabelColor = fontColor;
                            GUIStyle labelstyle;
                            if (go.activeInHierarchy)
                            {
                                labelstyle = GetTreeViewStyle_LineStyle();
                            }
                            else
                            {
                                labelstyle = GetGameObjectStyle_DisabledLabel();
                            }

                            //Draw Background
                            //var skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
                            var backColor = GetDefaultBackgroundColor();
                            EditorGUI.DrawRect(offsetRect, backColor);
                            if (win.SceneHierarchy.TreeView.IsItemDragSelectedOrSelected(item))
                            {
                                var focusWin = EditorWindow.focusedWindow;
                                bool focused = focusWin == win.Raw;
                                var style = GetHierarchyStyle_Selection();
                                style.Draw(offsetRect, false, false, true, focused);
                                if (focused)
                                {
                                    defaultLabelColor = labelstyle.onFocused.textColor;
                                }
                                else
                                {
                                    defaultLabelColor = labelstyle.onNormal.textColor;
                                }
                            }
                            else
                            {
                                if (win.SceneHierarchy.TreeView.HoveredItem == item)
                                {
                                    EditorGUI.DrawRect(offsetRect, GetGameObjectStyle_HoveredBackgroundColor());
                                }
                                defaultLabelColor = labelstyle.normal.textColor;
                            }

                            // Draw name label
                            fontColor = (fontColor + defaultLabelColor) / 2;
                            fontColor.a = defaultLabelColor.a;

                            EditorGUI.LabelField(offsetRect, obj.name, new GUIStyle()
                            {
                                normal = new GUIStyleState() { textColor = fontColor },
                            });

                            // overdraw name label
                            //Rect[] overdrawRects = new[]
                            //{
                            //    new Rect(offsetRect.x - 1, offsetRect.y, offsetRect.width, offsetRect.height),
                            //    //new Rect(offsetRect.x, offsetRect.y - 1, offsetRect.width, offsetRect.height),
                            //    //new Rect(offsetRect.x + 1, offsetRect.y, offsetRect.width, offsetRect.height),
                            //    //new Rect(offsetRect.x, offsetRect.y + 1, offsetRect.width, offsetRect.height),
                            //};
                            //for (int i = 0; i < overdrawRects.Length; ++i)
                            //{
                            //    EditorGUI.LabelField(overdrawRects[i], obj.name, new GUIStyle()
                            //    {
                            //        normal = new GUIStyleState() { textColor = fontColor },
                            //    });
                            //}
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