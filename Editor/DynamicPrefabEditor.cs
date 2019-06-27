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
    [InitializeOnLoad]
    public static class DynamicPrefabEditor
    {
        static DynamicPrefabEditor()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += DynamicPrefabPostprocessor.SceneSavingCallback;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += DynamicPrefabPostprocessor.SceneSavedCallback;
            
            //EditorBridge.PrePlayModeChange += () =>
            //{
            //    //DynamicPrefabPostprocessor.DestroyDynamicChildInAllScenes();
            //};
        }

        //private class DynamicPrefabModificationProcessor : UnityEditor.AssetModificationProcessor
        //{
        //    static string[] OnWillSaveAssets(string[] paths)
        //    {
        //        Debug.Log("OnWillSaveAssets");
        //        foreach (string path in paths)
        //            Debug.Log(path);
        //        return paths;
        //    }
        //}

        private class DynamicPrefabPostprocessor : AssetPostprocessor
        {
            private static HashSet<string> _LastModifiedPrefabs = new HashSet<string>();
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                // Prepare
                DynamicPrefab.ForbidLoadDynamicChild = true;
                //var oldstage = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle();
                //bool oldisscene = IsStageScene(oldstage);
                var lastmodified = _LastModifiedPrefabs;
                _LastModifiedPrefabs = new HashSet<string>();
                // Process
                if (importedAssets != null)
                {
                    for (int i = 0; i < importedAssets.Length; ++i)
                    {
                        var asset = importedAssets[i];
                        if (!lastmodified.Contains(asset))
                        {
                            if (asset.EndsWith(".prefab"))
                            {
                                if (CheckPrefab(asset))
                                {
                                    _LastModifiedPrefabs.Add(asset);
                                }
                            }
                        }
                    }
                }
                // TODO: check prefab variant's duplicated stub.
                // but it seems that we do not need do this. Because same child in variant is recognized as the same obj of parent's.
                // Restore
                //if (oldisscene)
                //{
                //    UnityEditor.SceneManagement.StageUtility.GoToMainStage();
                //}
                // sync scene?
                if (_LastModifiedPrefabs.Count > 0)
                {
                    SyncSceneObjsAccordingToLastModified();
                }
                else
                {
                    DynamicPrefab.ForbidLoadDynamicChild = false;
                }
            }

            private static bool CheckPrefab(string path)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) as GameObject;
                if (prefab)
                {
                    bool changed = false;
                    bool isvariant = PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant;
                    if (isvariant)
                    {
                        if (!changed)
                        {
                            var mods = PrefabUtility.GetPropertyModifications(prefab);
                            if (mods != null)
                            {
                                changed = mods.Any(mod => mod.target is DynamicPrefab && mod.propertyPath == "DynamicChild");
                            }
                        }
                        if (!changed)
                        {
                            var comps = PrefabUtility.GetAddedComponents(prefab);
                            if (comps != null)
                            {
                                changed = comps.Any(comp => comp.instanceComponent is DynamicPrefab && ((DynamicPrefab)comp.instanceComponent).DynamicChild);
                            }
                        }
                        if (!changed)
                        {
                            var gos = PrefabUtility.GetAddedGameObjects(prefab);
                            if (gos != null)
                            {
                                changed = gos.Any(go =>
                                {
                                    var comps = go.instanceGameObject.GetComponents<DynamicPrefab>();
                                    return comps != null && comps.Any(comp => comp.DynamicChild);
                                });
                            }
                        }
                    }
                    else
                    {
                        var pars = prefab.GetComponentsInChildren<DynamicPrefab>(true);
                        if (pars != null)
                        {
                            for (int j = 0; j < pars.Length; ++j)
                            {
                                var par = pars[j];
                                if (par)
                                {
                                    var src = PrefabUtility.GetNearestPrefabInstanceRoot(par);
                                    if (!src || PrefabUtility.GetCorrespondingObjectFromSource(src) == prefab)
                                    {
                                        changed = par.DynamicChild;
                                        if (changed)
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        var mods = PrefabUtility.GetPropertyModifications(par.gameObject);
                                        if (mods != null)
                                        {
                                            changed = mods.Any(mod => mod.target is DynamicPrefab && mod.propertyPath == "DynamicChild");
                                            if (changed)
                                            {
                                                break;
                                            }
                                        }
                                        var comps = PrefabUtility.GetAddedComponents(par.gameObject);
                                        if (comps != null)
                                        {
                                            changed = comps.Any(comp => comp.instanceComponent is DynamicPrefab && ((DynamicPrefab)comp.instanceComponent).DynamicChild);
                                            if (changed)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (changed)
                    {
                        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
                        var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

                        if (isvariant)
                        {
                            var mods = PrefabUtility.GetPropertyModifications(prefab);
                            if (mods != null && mods.Length > 0)
                            {
                                Dictionary<DynamicPrefab, DynamicPrefab> prefab2instance = GetPrefabToInstanceDict<DynamicPrefab>(instance);
                                for (int i = 0; i < mods.Length; ++i)
                                {
                                    var mod = mods[i];
                                    if (mod.target is DynamicPrefab && mod.propertyPath == "DynamicChild")
                                    {
                                        if (prefab2instance.ContainsKey(mod.target as DynamicPrefab))
                                        {
                                            var minst = prefab2instance[mod.target as DynamicPrefab];
                                            try
                                            {
                                                PrefabUtility.RevertAddedGameObject(minst.DynamicChild, InteractionMode.AutomatedAction);
                                            }
                                            catch { }
                                            PrefabUtility.RevertPropertyOverride(new SerializedObject(minst).FindProperty("DynamicChild"), InteractionMode.AutomatedAction);
                                        }
                                    }
                                }
                            }
                            var comps = PrefabUtility.GetAddedComponents(prefab);
                            if (comps != null)
                            {
                                for (int i = 0; i < comps.Count; ++i)
                                {
                                    var comp = comps[i];
                                    if (comp.instanceComponent is DynamicPrefab && ((DynamicPrefab)comp.instanceComponent).DynamicChild)
                                    {
                                        Object.DestroyImmediate(((DynamicPrefab)comp.instanceComponent).DynamicChild);
                                        ((DynamicPrefab)comp.instanceComponent).DynamicChild = null;
                                    }
                                }
                            }
                            var gos = PrefabUtility.GetAddedGameObjects(prefab);
                            if (gos != null)
                            {
                                for (int i = 0; i < gos.Count; ++i)
                                {
                                    var go = gos[i];
                                    var ccomps = go.instanceGameObject.GetComponents<DynamicPrefab>();
                                    if (ccomps != null)
                                    {
                                        for (int j = 0; j < ccomps.Length; ++j)
                                        {
                                            var comp = ccomps[j];
                                            if (comp.DynamicChild)
                                            {
                                                Object.DestroyImmediate(comp.DynamicChild);
                                                comp.DynamicChild = null;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var pars = instance.GetComponentsInChildren<DynamicPrefab>();
                            if (pars != null)
                            {
                                for (int j = 0; j < pars.Length; ++j)
                                {
                                    var par = pars[j];
                                    if (par)
                                    {
                                        if (PrefabUtility.GetPrefabAssetType(par) == PrefabAssetType.NotAPrefab)
                                        {
                                            if (par.DynamicChild)
                                            {
                                                Object.DestroyImmediate(par.DynamicChild);
                                                par.DynamicChild = null;
                                            }
                                        }
                                        else
                                        {
                                            var mods = PrefabUtility.GetPropertyModifications(par.gameObject);
                                            if (mods != null && mods.Length > 0)
                                            {
                                                Dictionary<DynamicPrefab, DynamicPrefab> prefab2instance = GetPrefabToInstanceDict<DynamicPrefab>(par.gameObject);
                                                for (int i = 0; i < mods.Length; ++i)
                                                {
                                                    var mod = mods[i];
                                                    if (mod.target is DynamicPrefab && mod.propertyPath == "DynamicChild")
                                                    {
                                                        if (prefab2instance.ContainsKey(mod.target as DynamicPrefab))
                                                        {
                                                            var minst = prefab2instance[mod.target as DynamicPrefab];
                                                            try
                                                            {
                                                                PrefabUtility.RevertAddedGameObject(minst.DynamicChild, InteractionMode.AutomatedAction);
                                                            }
                                                            catch { }
                                                            PrefabUtility.RevertPropertyOverride(new SerializedObject(minst).FindProperty("DynamicChild"), InteractionMode.AutomatedAction);
                                                        }
                                                    }
                                                }
                                            }
                                            var comps = PrefabUtility.GetAddedComponents(par.gameObject);
                                            if (comps != null)
                                            {
                                                for (int i = 0; i < comps.Count; ++i)
                                                {
                                                    var comp = comps[i];
                                                    if (comp.instanceComponent is DynamicPrefab && ((DynamicPrefab)comp.instanceComponent).DynamicChild)
                                                    {
                                                        Object.DestroyImmediate(((DynamicPrefab)comp.instanceComponent).DynamicChild);
                                                        ((DynamicPrefab)comp.instanceComponent).DynamicChild = null;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        PrefabUtility.SaveAsPrefabAsset(instance, path);
                        UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
                        
                        //// open prefab in editor
                        //AssetDatabase.OpenAsset(prefab);
                        //var stage = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle();
                        //// find the root instance.
                        //var instance = stage.FindComponentOfType<Transform>().gameObject;
                        //while (instance && (instance.hideFlags & HideFlags.DontSave) != 0)
                        //{
                        //    if (instance.transform.childCount > 0)
                        //    {
                        //        instance = instance.transform.GetChild(0).gameObject;
                        //    }
                        //    else
                        //    {
                        //        instance = null;
                        //    }
                        //}
                        //// delete dynamic child.
                        //if (instance)
                        //{
                        //    if (isvariant)
                        //    {
                        //        var mods = PrefabUtility.GetPropertyModifications(prefab);
                        //        if (mods != null && mods.Length > 0)
                        //        {
                        //            Dictionary<DynamicPrefab, DynamicPrefab> prefab2instance = GetPrefabToInstanceDict<DynamicPrefab>(instance);
                        //            for (int i = 0; i < mods.Length; ++i)
                        //            {
                        //                var mod = mods[i];
                        //                if (mod.target is DynamicPrefab && mod.propertyPath == "DynamicChild")
                        //                {
                        //                    if (prefab2instance.ContainsKey(mod.target as DynamicPrefab))
                        //                    {
                        //                        var minst = prefab2instance[mod.target as DynamicPrefab];
                        //                        try
                        //                        {
                        //                            PrefabUtility.RevertAddedGameObject(minst.DynamicChild, InteractionMode.AutomatedAction);
                        //                        }
                        //                        catch { }
                        //                        PrefabUtility.RevertPropertyOverride(new SerializedObject(minst).FindProperty("DynamicChild"), InteractionMode.AutomatedAction);
                        //                    }
                        //                }
                        //            }
                        //        }
                        //        var comps = PrefabUtility.GetAddedComponents(prefab);
                        //        if (comps != null)
                        //        {
                        //            for (int i = 0; i < comps.Count; ++i)
                        //            {
                        //                var comp = comps[i];
                        //                if (comp.instanceComponent is DynamicPrefab && ((DynamicPrefab)comp.instanceComponent).DynamicChild)
                        //                {
                        //                    Object.DestroyImmediate(((DynamicPrefab)comp.instanceComponent).DynamicChild);
                        //                    ((DynamicPrefab)comp.instanceComponent).DynamicChild = null;
                        //                }
                        //            }
                        //        }
                        //        var gos = PrefabUtility.GetAddedGameObjects(prefab);
                        //        if (gos != null)
                        //        {
                        //            for (int i = 0; i < gos.Count; ++i)
                        //            {
                        //                var go = gos[i];
                        //                var ccomps = go.instanceGameObject.GetComponents<DynamicPrefab>();
                        //                if (ccomps != null)
                        //                {
                        //                    for (int j = 0; j < ccomps.Length; ++j)
                        //                    {
                        //                        var comp = ccomps[j];
                        //                        if (comp.DynamicChild)
                        //                        {
                        //                            Object.DestroyImmediate(comp.DynamicChild);
                        //                            comp.DynamicChild = null;
                        //                        }
                        //                    }
                        //                }
                        //            }
                        //        }
                        //    }
                        //    else
                        //    {
                        //        var pars = instance.GetComponentsInChildren<DynamicPrefab>();
                        //        if (pars != null)
                        //        {
                        //            for (int j = 0; j < pars.Length; ++j)
                        //            {
                        //                var par = pars[j];
                        //                if (par)
                        //                {
                        //                    if (PrefabUtility.GetPrefabAssetType(par) == PrefabAssetType.NotAPrefab)
                        //                    {
                        //                        if (par.DynamicChild)
                        //                        {
                        //                            Object.DestroyImmediate(par.DynamicChild);
                        //                            par.DynamicChild = null;
                        //                        }
                        //                    }
                        //                    else
                        //                    {
                        //                        var mods = PrefabUtility.GetPropertyModifications(par.gameObject);
                        //                        if (mods != null && mods.Length > 0)
                        //                        {
                        //                            Dictionary<DynamicPrefab, DynamicPrefab> prefab2instance = GetPrefabToInstanceDict<DynamicPrefab>(par.gameObject);
                        //                            for (int i = 0; i < mods.Length; ++i)
                        //                            {
                        //                                var mod = mods[i];
                        //                                if (mod.target is DynamicPrefab && mod.propertyPath == "DynamicChild")
                        //                                {
                        //                                    if (prefab2instance.ContainsKey(mod.target as DynamicPrefab))
                        //                                    {
                        //                                        var minst = prefab2instance[mod.target as DynamicPrefab];
                        //                                        try
                        //                                        {
                        //                                            PrefabUtility.RevertAddedGameObject(minst.DynamicChild, InteractionMode.AutomatedAction);
                        //                                        }
                        //                                        catch { }
                        //                                        PrefabUtility.RevertPropertyOverride(new SerializedObject(minst).FindProperty("DynamicChild"), InteractionMode.AutomatedAction);
                        //                                    }
                        //                                }
                        //                            }
                        //                        }
                        //                        var comps = PrefabUtility.GetAddedComponents(par.gameObject);
                        //                        if (comps != null)
                        //                        {
                        //                            for (int i = 0; i < comps.Count; ++i)
                        //                            {
                        //                                var comp = comps[i];
                        //                                if (comp.instanceComponent is DynamicPrefab && ((DynamicPrefab)comp.instanceComponent).DynamicChild)
                        //                                {
                        //                                    Object.DestroyImmediate(((DynamicPrefab)comp.instanceComponent).DynamicChild);
                        //                                    ((DynamicPrefab)comp.instanceComponent).DynamicChild = null;
                        //                                }
                        //                            }
                        //                        }
                        //                    }
                        //                }
                        //            }
                        //        }
                        //    }
                        //    PrefabUtility.SaveAsPrefabAsset(instance, path);
                        //}
                    }
                    return changed;
                }
                return false;
            }

            private static Dictionary<T, T> GetPrefabToInstanceDict<T>(GameObject root) where T : Component
            {
                Dictionary<T, T> results = new Dictionary<T, T>();
                var comps = root.GetComponentsInChildren<T>(true);
                if (comps != null)
                {
                    for (int i = 0; i < comps.Length; ++i)
                    {
                        var comp = comps[i];
                        try
                        {
                            var asset = PrefabUtility.GetCorrespondingObjectFromSource(comp);
                            if (asset)
                            {
                                results[asset] = comp;
                            }
                        }
                        catch { }
                    }
                }
                return results;
            }

            private static void SyncSceneObjsAccordingToLastModified()
            {
                for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; ++i)
                {
                    var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                    if (scene.IsValid())
                    {
                        var gos = scene.GetRootGameObjects();
                        if (gos != null)
                        {
                            for (int j = 0; j < gos.Length; ++j)
                            {
                                var go = gos[j];
                                if (go)
                                {
                                    var pars = go.GetComponentsInChildren<DynamicPrefab>(true);
                                    if (pars != null)
                                    {
                                        for (int k = 0; k < pars.Length; ++k)
                                        {
                                            var par = pars[k];
                                            if (par)
                                            {
                                                if (PrefabUtility.GetPrefabInstanceStatus(par) != PrefabInstanceStatus.NotAPrefab)
                                                {
                                                    bool changed = false;
                                                    var src = PrefabUtility.GetCorrespondingObjectFromSource(par);
                                                    while (src)
                                                    {
                                                        var path = AssetDatabase.GetAssetPath(src);
                                                        if (!string.IsNullOrEmpty(path))
                                                        {
                                                            if (_LastModifiedPrefabs.Contains(path))
                                                            {
                                                                changed = true;
                                                                break;
                                                            }
                                                        }
                                                        if (PrefabUtility.GetPrefabAssetType(src) == PrefabAssetType.Variant)
                                                        {
                                                            src = PrefabUtility.GetCorrespondingObjectFromSource(src);
                                                        }
                                                        else
                                                        {
                                                            src = null;
                                                        }
                                                    }
                                                    if (changed)
                                                    {
                                                        try
                                                        {
                                                            PrefabUtility.RevertAddedGameObject(par.DynamicChild, InteractionMode.AutomatedAction);
                                                            PrefabUtility.RevertPropertyOverride(new SerializedObject(par).FindProperty("DynamicChild"), InteractionMode.AutomatedAction);
                                                        }
                                                        catch { }
                                                        CheckDuplicatedStub(par.gameObject);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            private static bool CheckDuplicatedStub(GameObject root)
            {
                if (root)
                {
                    var pars = root.GetComponentsInChildren<DynamicPrefab>(true);
                    if (pars != null)
                    {
                        for (int k = 0; k < pars.Length; ++k)
                        {
                            var par = pars[k];
                            if (PrefabUtility.GetPrefabInstanceStatus(root) != PrefabInstanceStatus.NotAPrefab)
                            {
                                var src = PrefabUtility.GetCorrespondingObjectFromSource(par);
                                if (src)
                                {
                                    Dictionary<string, DataStubBase> srcStubs = new Dictionary<string, DataStubBase>();
                                    var trans = src.transform;
                                    for (int i = 0; i < trans.childCount; ++i)
                                    {
                                        var child = trans.GetChild(i);
                                        var comp = child.GetComponent<DataStubBase>();
                                        if (comp)
                                        {
                                            srcStubs[child.name] = comp;
                                        }
                                    }

                                    if (srcStubs.Count > 0)
                                    {
                                        List<DataStubBase> dupStubs = new List<DataStubBase>();
                                        trans = par.transform;
                                        for (int i = 0; i < trans.childCount; ++i)
                                        {
                                            var child = trans.GetChild(i);
                                            var comp = child.GetComponent<DataStubBase>();
                                            if (comp)
                                            {
                                                if (srcStubs.ContainsKey(child.name))
                                                {
                                                    var srcStub = srcStubs[child.name];
                                                    if (PrefabUtility.GetCorrespondingObjectFromSource(comp) != srcStub)
                                                    {
                                                        dupStubs.Add(comp);
                                                    }
                                                }
                                            }
                                        }
                                        if (dupStubs.Count > 0)
                                        {
                                            for (int i = 0; i < dupStubs.Count; ++i)
                                            {
                                                var stub = dupStubs[i];
                                                try
                                                {
                                                    PrefabUtility.RevertAddedGameObject(stub.gameObject, InteractionMode.AutomatedAction);
                                                }
                                                catch { }
                                            }
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return false;
            }

            public static void DestroyDynamicChildInAllScenes()
            {
                for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; ++i)
                {
                    var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                    if (scene.IsValid())
                    {
                        DestroyDynamicChildInScene(scene);
                    }
                }
            }
            public static void SceneSavingCallback(UnityEngine.SceneManagement.Scene scene, string path)
            {
                DestroyDynamicChildInScene(scene);
            }
            public static void DestroyDynamicChildInScene(UnityEngine.SceneManagement.Scene scene)
            {
                var gos = scene.GetRootGameObjects();
                if (gos != null)
                {
                    for (int i = 0; i < gos.Length; ++i)
                    {
                        var go = gos[i];
                        if (go)
                        {
                            var pars = go.GetComponentsInChildren<DynamicPrefab>(true);
                            if (pars != null)
                            {
                                for (int j = 0; j < pars.Length; ++j)
                                {
                                    var par = pars[j];
                                    if (par)
                                    {
                                        if (PrefabUtility.GetPrefabInstanceStatus(par) != PrefabInstanceStatus.NotAPrefab)
                                        {
                                            try
                                            {
                                                PrefabUtility.RevertAddedGameObject(par.DynamicChild, InteractionMode.AutomatedAction);
                                                PrefabUtility.RevertPropertyOverride(new SerializedObject(par).FindProperty("DynamicChild"), InteractionMode.AutomatedAction);
                                            }
                                            catch { }
                                        }
                                        else
                                        {
                                            par.DestroyDynamicChild();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            public static void LoadDynamicChildInAllScenes()
            {
                for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; ++i)
                {
                    var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                    if (scene.IsValid())
                    {
                        LoadDynamicChildInScene(scene);
                    }
                }
            }
            public static void SceneSavedCallback(UnityEngine.SceneManagement.Scene scene)
            {
                LoadDynamicChildInScene(scene);
            }
            public static void LoadDynamicChildInScene(UnityEngine.SceneManagement.Scene scene)
            {
                var gos = scene.GetRootGameObjects();
                if (gos != null)
                {
                    for (int i = 0; i < gos.Length; ++i)
                    {
                        var go = gos[i];
                        if (go)
                        {
                            var pars = go.GetComponentsInChildren<DynamicPrefab>(false);
                            if (pars != null)
                            {
                                for (int j = 0; j < pars.Length; ++j)
                                {
                                    var par = pars[j];
                                    if (par)
                                    {
                                        par.LoadDynamicChild();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
