using UnityEngine;

namespace Capstones.UnityEngineEx
{
    public class DynamicMaterialTexture : MonoBehaviour
    {
        public string[] MatProps;
        public string[] Paths;
        bool Loaded;

        void OnEnable()
        {
            if (!Loaded)
            {
                Loaded = true;
                LoadMaterialTexture();
            }
        }

        private void LoadMaterialTexture()
        {
            var mesh = GetComponent<MeshRenderer>();
            if (mesh == null)
                return;

            for (int i = 0; i < Paths.Length; i++)
            {
                var path = Paths[i];
                var tex = ResManager.LoadRes<Texture>(path);
                if (tex == null)
                    continue;

                var prop = new MaterialPropertyBlock();
                prop.SetTexture(MatProps[i], tex);
                mesh.SetPropertyBlock(prop);
            }
        }
    }
}