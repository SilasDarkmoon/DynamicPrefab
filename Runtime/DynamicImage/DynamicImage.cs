using System;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    [ExecuteInEditMode]
    public class DynamicImage : MonoBehaviour
    {
        public string Path;
        public bool OnlyLoadWhenEmpty;
        private Object _Loaded = null;
        public Image.Type ImageType;
        public bool PreserveAspect = false;
        public Image.FillMethod FillMethod;
        public int FillOrigin;
        public float FillAmount = 1;
        public bool FillClockwise = true;
        public bool FillCenter = true;
        public bool IsNativeSize = false;

#if UNITY_EDITOR && !USE_CLIENT_RES_MANAGER
        private Image _TargetImage;
        private RawImage _TargetRawImage;
#endif

        public void ApplySource()
        {
            DestroyDynamicChild();
            _Loaded = null;
            if (!string.IsNullOrEmpty(Path))
            {
                var image = GetComponent<Image>();
                if (image)
                {
                    var sprite = ResManager.LoadRes<Sprite>(Path);
                    if (sprite)
                    {
                        _Loaded = sprite;
                        image.overrideSprite = sprite;
                        image.type = ImageType;
                        if (ImageType == Image.Type.Filled)
                        {
                            image.fillMethod = FillMethod;
                            image.fillOrigin = FillOrigin;
                            image.fillAmount = FillAmount;
                            image.fillClockwise = FillClockwise;
                        }
                        if (ImageType == Image.Type.Simple || ImageType == Image.Type.Filled)
                        {
                            image.preserveAspect = PreserveAspect;
                        }
                        else
                        {
                            image.fillCenter = FillCenter;
                        }
                        if (IsNativeSize && image.type == Image.Type.Simple)
                        {
                            image.SetNativeSize();
                        }
                    }
                }
                var rawimage = GetComponent<RawImage>();
                if (rawimage)
                {
                    var texture = ResManager.LoadRes<Texture>(Path);
                    if (texture)
                    {
                        _Loaded = texture;
                        rawimage.texture = texture;
                    }
                }
            }
        }

        public void DestroyDynamicChild()
        {
            var image = GetComponent<Image>();
            if (image)
            {
                image.overrideSprite = null;
            }
            var rawimage = GetComponent<RawImage>();
            if (rawimage)
            {
                rawimage.texture = null;
            }
        }

        public void SetMaterialDirty()
        {
            var image = GetComponent<Image>();
            if (image)
            {
                image.SetMaterialDirty();
            }
            var rawimage = GetComponent<RawImage>();
            if (rawimage)
            {
                rawimage.SetMaterialDirty();
            }
        }

        public bool IsImageEmpty()
        {
            var image = GetComponent<Image>();
            if (image)
            {
                if (image.overrideSprite)
                {
                    return false;
                }
            }
            var rawimage = GetComponent<RawImage>();
            if (rawimage)
            {
                if (rawimage.texture)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsImageDirty()
        {
#if UNITY_EDITOR && !USE_CLIENT_RES_MANAGER
            var image = _TargetImage;
            if (!Application.isPlaying)
            {
                image = GetComponent<Image>();
            }
            if (image)
            {
                if (!image.overrideSprite && _Loaded)
                {
                    return true;
                }
                else if (image.overrideSprite && image.overrideSprite != _Loaded)
                {
                    return true;
                }
            }
            var rawimage = _TargetRawImage;
            if (!Application.isPlaying)
            {
                rawimage = GetComponent<RawImage>();
            }
            if (rawimage)
            {
                if (rawimage.texture != _Loaded)
                {
                    return true;
                }
            }
#endif
            return false;
        }

        void Update()
        {
#if UNITY_EDITOR && !USE_CLIENT_RES_MANAGER
            if (IsImageDirty())
            {
                if (Application.isEditor && !Application.isPlaying || !OnlyLoadWhenEmpty || IsImageEmpty())
                {
                    ApplySource();
                }
            }
            else
            {
                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                if (prefab)
                {
                    var path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var rawimage = prefab.GetComponent<RawImage>();
                        if (rawimage)
                        {
                            if (rawimage.texture)
                            {
                                SavePrefabWithoutChild();
                                ApplySource();
                            }
                        }
                    }
                }
            }
#endif
        }

        public void SavePrefabWithoutChild()
        {
#if UNITY_EDITOR && !USE_CLIENT_RES_MANAGER
            DestroyDynamicChild();
            var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefab)
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
                if (!string.IsNullOrEmpty(path))
                {
                    var root = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                    UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, UnityEditor.InteractionMode.AutomatedAction);
                }
            }
#endif
        }
        public void RebuildImage()
        {
#if UNITY_EDITOR && !USE_CLIENT_RES_MANAGER
            var image = GetComponent<Image>();
            if (image)
            {
                image.enabled = false;
                image.enabled = true;
            }
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR && !USE_CLIENT_RES_MANAGER
            if (!Application.isPlaying)
            {
                DestroyDynamicChild();
            }
#endif
        }
        void OnEnable()
        {
#if UNITY_EDITOR && !USE_CLIENT_RES_MANAGER
            if (!Application.isPlaying)
            {
                if (IsImageDirty() || !OnlyLoadWhenEmpty || IsImageEmpty())
                {
                    ApplySource();
                }
            }
            else
            {
                if ((!_Loaded && !OnlyLoadWhenEmpty) || (IsImageEmpty() && OnlyLoadWhenEmpty))
                {
                    ApplySource();
                }
            }
#else
            if ((!_Loaded && !OnlyLoadWhenEmpty) || (IsImageEmpty() && OnlyLoadWhenEmpty))
            {
                ApplySource();
            }
#endif
        }

        void Awake()
        {
            var image = GetComponent<Image>();
            if (image)
            {
                if (ImageType != image.type)
                {
                    ImageType = image.type;
                }
                if (ImageType == Image.Type.Simple || ImageType == Image.Type.Filled)
                {
                    if (PreserveAspect != image.preserveAspect)
                    {
                        PreserveAspect = image.preserveAspect;
                    }
                }
                else
                {
                    if (FillCenter != image.fillCenter)
                    {
                        FillCenter = image.fillCenter;
                    }
                }
                if (ImageType == Image.Type.Filled)
                {
                    if (FillMethod != image.fillMethod)
                    {
                        FillMethod = image.fillMethod;
                    }
                    if (FillOrigin != image.fillOrigin)
                    {
                        FillOrigin = image.fillOrigin;
                    }
                    if (FillAmount != image.fillAmount)
                    {
                        FillAmount = image.fillAmount;
                    }
                    if (FillClockwise != image.fillClockwise)
                    {
                        FillClockwise = image.fillClockwise;
                    }
                }
            }
#if UNITY_EDITOR && !USE_CLIENT_RES_MANAGER
            _TargetRawImage = null;
            _TargetImage = null;
            if (Application.isPlaying)
            {
                _TargetRawImage = GetComponent<RawImage>();
                _TargetImage = image;
            }
#endif
        }

        void Start()
        {
#if UNITY_EDITOR && !USE_CLIENT_RES_MANAGER
            var rawimage = GetComponent<RawImage>();
            if (rawimage)
            {
                if (rawimage.texture)
                {
                    SavePrefabWithoutChild();
                }
            }
#endif
            if (!Application.isEditor || Application.isPlaying)
            {
                if ((!_Loaded && !OnlyLoadWhenEmpty) || (IsImageEmpty() && OnlyLoadWhenEmpty))
                {
                    ApplySource();
                }
                else
                {
                    SetMaterialDirty();
                }
            }
            else
            {
                ApplySource();
            }
        }

        public bool IsActive()
        {
            return enabled && isActiveAndEnabled && gameObject.activeInHierarchy;
        }

        public void SetNativeSize()
        {
            var image = GetComponent<Image>();
            if (image)
            {
                if (ImageType == Image.Type.Simple || ImageType == Image.Type.Filled)
                {
                    image.SetNativeSize();
                }
            }
        }
    }
}