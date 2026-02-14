using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;

namespace SharedTexHub.Logic.Scanner
{
    public class MatCapScanner : ITextureScanner
    {
        public IEnumerable<TextureInfo> Scan(Material material)
        {
            if (material == null) yield break;

            // MatCap 1
            if (IsPropertyEnabled(material, "_UseMatCap"))
            {
                var tex = material.GetTexture("_MatCapTex");
                if (tex != null)
                {
                    string path = AssetDatabase.GetAssetPath(tex);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    yield return new TextureInfo(guid, path, Category.MatCap);
                }
            }

            // MatCap 2nd
            if (IsPropertyEnabled(material, "_UseMatCap2nd"))
            {
                var tex = material.GetTexture("_MatCap2ndTex");
                if (tex != null)
                {
                    string path = AssetDatabase.GetAssetPath(tex);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    yield return new TextureInfo(guid, path, Category.MatCap);
                }
            }
        }

        private bool IsPropertyEnabled(Material mat, string propertyName)
        {
            if (!mat.HasProperty(propertyName)) return false;
            return mat.GetFloat(propertyName) != 0;
        }
    }
}
