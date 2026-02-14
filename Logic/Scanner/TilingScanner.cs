using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;

namespace SharedTexHub.Logic.Scanner
{
    public class TilingScanner : ITextureScanner
    {
        public IEnumerable<TextureInfo> Scan(Material material)
        {
            if (material == null) yield break;

            // MainTex (Always check, but only if Tiling != 1,1)
            // Note: lilToon might not have a toggle for MainTex, or it might be implicit.
            CheckAndYield(material, "_MainTex", null, ref output);

            // Main2nd
            if (!IsDecal(material, "_Main2ndTexIsDecal"))
            {
                CheckAndYield(material, "_Main2ndTex", "_UseMain2ndTex", ref output);
            }

            // Main3rd
            if (!IsDecal(material, "_Main3rdTexIsDecal"))
            {
                CheckAndYield(material, "_Main3rdTex", "_UseMain3rdTex", ref output);
            }

            // Emission
            CheckAndYield(material, "_EmissionMap", "_UseEmission", ref output);

            // Emission 2nd
            CheckAndYield(material, "_Emission2ndMap", "_UseEmission2nd", ref output);

            foreach (var item in output)
            {
                yield return item;
            }
            output.Clear();
        }

        private List<TextureInfo> output = new List<TextureInfo>();

        private bool IsDecal(Material mat, string decalProp)
        {
            return mat.HasProperty(decalProp) && mat.GetFloat(decalProp) != 0;
        }

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
            list.Add(new TextureInfo(guid, path, Category.Tiling));
        }
    }
}
