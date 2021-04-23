using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicPrefabEditor")]

namespace Capstones.UnityEngineEx
{
    [ExecuteInEditMode]
    public class DynamicPrefab : MonoBehaviour//, ISerializationCallbackReceiver
    {
        [SerializeField]
        [HideInInspector]
        protected internal GameObject DynamicChild;

        public string Source;

        public IDictionary<string, object> ChildData { get; protected internal set; }

#if UNITY_EDITOR
        private string OldSource;
        public static bool ForbidLoadDynamicChild = false;
#endif
        private void Awake()
        {
#if UNITY_EDITOR
            OldSource = Source;
#endif
            LoadDynamicChild();
        }
        //public void OnAfterDeserialize()
        //{
        //}
        //public void OnBeforeSerialize()
        //{
        //}

#if UNITY_EDITOR
        private void Update()
        {
            LoadDynamicChild();
        }
#endif

        public void LoadDynamicChild()
        {
#if UNITY_EDITOR
            if (OldSource != Source)
            {
                DestroyDynamicChild();
                OldSource = Source;
            }
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                DestroyDynamicChild();
                return;
            }
            if (ForbidLoadDynamicChild)
            {
                return;
            }
#endif
            if (!DynamicChild && !string.IsNullOrEmpty(Source))
            {
                var prefab = ResManager.LoadRes(Source) as GameObject;
                if (prefab)
                {
#if UNITY_EDITOR
                    if (ResManager.ResLoader is ResManager.ClientResLoader)
                    {
                        DynamicChild = Instantiate(prefab);
                    }
                    else
                    {
                        DynamicChild = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;

                        if (UnityEditor.PrefabUtility.GetPrefabInstanceStatus(this) != UnityEditor.PrefabInstanceStatus.NotAPrefab)
                        {
                            List<UnityEditor.PropertyModification> mods = new List<UnityEditor.PropertyModification>(UnityEditor.PrefabUtility.GetPropertyModifications(this));
                            mods.Add(new UnityEditor.PropertyModification()
                            {
                                target = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(this),
                                propertyPath = "DynamicChild",
                                objectReference = DynamicChild,
                            });
                            UnityEditor.PrefabUtility.SetPropertyModifications(this, mods.ToArray());
                        }
                    }
#else
                    DynamicChild = Instantiate(prefab);
#endif
                    DynamicChild.transform.SetParent(transform, false);

                    // SyncData and Trig OnDynamicChildLoaded
                    SyncChildData();
                }
            }
        }
        public void DestroyDynamicChild()
        {
            if (DynamicChild)
            {
#if UNITY_EDITOR
                DestroyImmediate(DynamicChild, true);
#else
                Destroy(DynamicChild);
#endif
                DynamicChild = null;
            }
        }

#if UNITY_EDITOR
        public void MergeStubFromSource()
        {
            if (!string.IsNullOrEmpty(Source))
            {
                var prefab = ResManager.LoadRes(Source) as GameObject;
                MergeStubFromPrefab(prefab);
            }
        }
        public void MergeStubFromPrefab(GameObject prefab)
        {
            if (prefab)
            {
                var provider = prefab.GetComponent<DataProviderBase>();
                if (provider)
                {
                    gameObject.MergeDynamicChildStubs(provider.GetData());
                }
            }
        }
        public void CreateStubFromPrefab()
        {
            GameObject prefab = null;
            if (!string.IsNullOrEmpty(Source))
            {
                prefab = ResManager.LoadRes(Source) as GameObject;
            }
            MergeStubFromPrefab(prefab);
        }
        public void CreateStubFromPrefab(GameObject prefab)
        {
            if (prefab)
            {
                var provider = prefab.GetComponent<DataProviderBase>();
                if (provider)
                {
                    gameObject.CreateDynamicChildStubs(provider.GetData());
                }
            }
        }
#endif

        public void SyncChildData()
        {
            IDictionary<string, object> childdata = null;
            if (DynamicChild)
            {
                var provider = DynamicChild.GetComponent<DataProviderBase>();
                if (provider)
                {
                    childdata = provider.GetData();
                }
            }
            ChildData = childdata;
            gameObject.SyncDynamicChildStubs(childdata);
            if (OnDynamicChildLoaded != null)
            {
                OnDynamicChildLoaded.Invoke(childdata);
            }
            var comps = GetComponents<Behaviour>();
            if (comps != null)
            {
                for (int i = 0; i < comps.Length; ++i)
                {
                    var comp = comps[i];
                    if (comp.isActiveAndEnabled && comp is IDataReceiver)
                    {
                        ((IDataReceiver)comp).Receive(childdata);
                    }
                }
            }
        }

        [Serializable]
        public class OnDynamicChildLoadedEvent : UnityEngine.Events.UnityEvent<IDictionary<string, object>>
        {
        }
        public OnDynamicChildLoadedEvent OnDynamicChildLoaded;
    }
}