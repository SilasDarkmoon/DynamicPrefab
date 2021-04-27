using System.Collections;
using UnityEngine;

namespace Capstones.UnityEngineEx
{
    public class DynamicMaterialTexture : MonoBehaviour
    {
        public string[] MatProps;
        public string[] Paths;
        bool Loaded;

        private void Start()
        {
            //StartCoroutine(Delay());
        }

        /// <summary>
        /// 在加载场景后需要延迟加载，原因未知
        /// </summary>
        /// <returns></returns>
        IEnumerator Delay()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            LoadMaterialTexture();
        }

        void OnEnable()
        {
            //if (!Loaded)
            //{
            //    Loaded = true;
            //    LoadMaterialTexture();
            //}
        }

        public void LoadMaterialTexture()
        {
            var meshs = GetComponents<MeshRenderer>();
            foreach (var mesh in meshs)
            {
                if (mesh == null)
                {
                    Debug.LogError("DynamicMaterialTexture::mesh is null. " + gameObject.name);
                    continue;
                }
                if (mesh.sharedMaterials.Length != 1)
                {
                    Debug.LogError("DynamicMaterialTexture::sharedMaterials is null. " + gameObject.name);
                    continue;
                }

                for (int i = 0; i < Paths.Length; i++)
                {
                    var path = Paths[i];
                    var tex = ResManager.LoadRes<Texture>(path);
                    if (tex == null)
                    {
                        Debug.LogError("DynamicMaterialTexture::tex is null. " + path);
                        continue;
                    }

                    //var prop = new MaterialPropertyBlock();
                    //mesh.GetPropertyBlock(prop);
                    //prop.SetTexture(MatProps[i], tex);
                    //mesh.SetPropertyBlock(prop);

                    //TODO: 临时方案，因为球场加载会出莫名其妙的Bug
                    mesh.sharedMaterial.SetTexture(MatProps[i], tex);
                }
            }
        }
    }
}
