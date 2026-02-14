using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;

namespace SharedTexHub.Logic.Scanner
{
    public class MaskScanner : ITextureScanner
    {
        public IEnumerable<TextureInfo> Scan(Material material)
        {
            if (material == null) yield break;

            // EmissionBlendMask
            CheckAndYield(material, "_EmissionBlendMask", "_UseEmission", ref output);

            // Emission2ndBlendMask
            CheckAndYield(material, "_Emission2ndBlendMask", "_UseEmission2nd", ref output);

            // AudioLinkMask
            CheckAndYield(material, "_AudioLinkMask", "_UseAudioLink", ref output);

            // DissolveMask
            // Dissolve doesn't seem to have a global toggle in lts.shader main block, 
            // but usually assumes it's used if params are set. 
            // We can check if property exists and texture is set.
            CheckAndYield(material, "_DissolveMask", null, ref output);

             // DissolveNoiseMask
            CheckAndYield(material, "_DissolveNoiseMask", null, ref output);

            // Main 2nd Dissolve
            CheckAndYield(material, "_Main2ndDissolveMask", "_UseMain2ndTex", ref output);
            CheckAndYield(material, "_Main2ndDissolveNoiseMask", "_UseMain2ndTex", ref output);

            // Main 3rd Dissolve
            CheckAndYield(material, "_Main3rdDissolveMask", "_UseMain3rdTex", ref output);
            CheckAndYield(material, "_Main3rdDissolveNoiseMask", "_UseMain3rdTex", ref output);

            // MatCap Mask
            // Note: These have [NoScaleOffset] in shader but internal _ST properties exist.
            CheckAndYield(material, "_MatCapBlendMask", "_UseMatCap", ref output);
            CheckAndYield(material, "_MatCap2ndBlendMask", "_UseMatCap2nd", ref output);

            // AlphaMask
            // _AlphaMaskMode != 0
            if (material.HasProperty("_AlphaMaskMode") && material.GetInt("_AlphaMaskMode") != 0)
            {
                 CheckAndYield(material, "_AlphaMask", null, ref output);
            }

            foreach (var item in output)
            {
                yield return item;
            }
            output.Clear();
        }

        private List<TextureInfo> output = new List<TextureInfo>();

        private void CheckAndYield(Material mat, string texProp, string toggleProp, ref List<TextureInfo> list)
        {
            // Toggle check
            if (toggleProp != null)
            {
                if (!mat.HasProperty(toggleProp) || mat.GetFloat(toggleProp) == 0) return;
            }

            // Texture property existence check
            if (!mat.HasProperty(texProp)) return;

            // Texture assignment check
            var tex = mat.GetTexture(texProp);
            if (tex == null) return;

             // Tiling check
             // Note: GetTextureScale might fail if the property is not strictly a Texture property with scale/offset?
             // lilToon defines them as 2D, so they should have scale/offset.
             // But sometimes [NoScaleOffset] is used. 
             // In lts.shader:
             // [NoScaleOffset] _AudioLinkMask ("Mask", 2D) = "blue" {}
             // [NoScaleOffset] _DissolveMask ("Dissolve Mask", 2D) = "white" {}
             // 
             // Wait, if they are [NoScaleOffset], then `GetTextureScale` might return (1,1) always or not be modifiable in UI.
             // The user request said: "independent tiling".
             // Let's re-read the implementation plan/user comments. 
             // User said: "Tiling判定のバリエーションで、マスク画像に専用タイリングがある部分も調査してほしいです。"
             // "Independent tiling" means the mask ITSELF has tiling properties.
             // 
             // _AudioLinkMask has [lilUVAnim] _AudioLinkMask_ScrollRotate ? No, wait.
             // Line 442: [NoScaleOffset] _AudioLinkMask
             // Line 443: [lilUVAnim] _AudioLinkMask_ScrollRotate
             // It seems lilToon handles tiling for these via separate vectors sometimes, or just doesn't support tiling for some.
             // _EmissionBlendMask defined at line 352: "Mask", 2D. It DOES NOT have [NoScaleOffset].
             // _Emission2ndBlendMask defined at 390. No [NoScaleOffset] there either.
             // so for Emission masks, GetTextureScale works.
             // 
             // For AudioLinkMask (442), it IS [NoScaleOffset].
             // But there is _AudioLinkMask_UVMode.
             // If UVMode is UV0/1/2/3, it uses mesh UVs.
             // 
             // Maybe I should only include masks that DO have Tiling != (1,1).
             // If a texture is [NoScaleOffset], Unity `GetTextureScale` typically returns (1,1).
             // So my check `scale == Vector2.one` will naturally exclude them if they don't support tiling.
             
            Vector2 scale = mat.GetTextureScale(texProp);
            if (scale == Vector2.one) return;

            string path = AssetDatabase.GetAssetPath(tex);
            string guid = AssetDatabase.AssetPathToGUID(path);
            list.Add(new TextureInfo(guid, path, Category.Mask));
        }
    }
}
