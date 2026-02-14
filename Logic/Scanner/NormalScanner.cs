using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;

namespace SharedTexHub.Logic.Scanner
{
    public class NormalScanner : ITextureScanner
    {
        public IEnumerable<TextureInfo> Scan(Material material)
        {
            if (material == null) yield break;

            // BumpMap
            CheckAndYield(material, "_BumpMap", "_UseBumpMap", ref output);

            // Bump2ndMap
            CheckAndYield(material, "_Bump2ndMap", "_UseBump2ndMap", ref output);

            foreach (var item in output)
            {
                yield return item;
            }
            output.Clear();
        }

        private List<TextureInfo> output = new List<TextureInfo>();

        private void CheckAndYield(Material mat, string texProp, string toggleProp, ref List<TextureInfo> list)
        {
            if (toggleProp != null)
            {
                if (!mat.HasProperty(toggleProp) || mat.GetFloat(toggleProp) == 0) return;
            }

            if (!mat.HasProperty(texProp)) return;

            var tex = mat.GetTexture(texProp);
            if (tex == null) return;

            Vector2 scale = mat.GetTextureScale(texProp);
            if (scale == Vector2.one) return; // Only interested if Tiling is NOT (1,1)

            string path = AssetDatabase.GetAssetPath(tex);
            string guid = AssetDatabase.AssetPathToGUID(path);
            list.Add(new TextureInfo(guid, path, Category.Normal));
        }
    }
}
