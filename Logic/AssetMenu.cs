using UnityEngine;
using UnityEditor;
using System.IO;
using SharedTexHub.Data;

namespace SharedTexHub.Logic
{
    public static class AssetMenu
    {
        [MenuItem("Assets/SharedTexHub/Add to Library/MatCap")]
        private static void AddToMatCap() => AddToLibrary(Category.MatCap);

        [MenuItem("Assets/SharedTexHub/Add to Library/Tiling")]
        private static void AddToTiling() => AddToLibrary(Category.Tiling);

        [MenuItem("Assets/SharedTexHub/Add to Library/Normal")]
        private static void AddToNormal() => AddToLibrary(Category.Normal);

        [MenuItem("Assets/SharedTexHub/Add to Library/Mask")]
        private static void AddToMask() => AddToLibrary(Category.Mask);

        [MenuItem("Assets/SharedTexHub/Add to Library/Decal")]
        private static void AddToDecal() => AddToLibrary(Category.Decal);

        private static void AddToLibrary(Category category)
        {
            DirectoryManager.EnsureCategoryFolder(category);
            string destFolder = DirectoryManager.GetCategoryFolderPath(category);

            foreach (var guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // If folder, process recursively? Or just copy folder? 
                // Plan said: "copy selected asset(s)".
                // If user selects a folder, we could iterate inside.
                // For MVP simplicity, let's handle files. If folder, maybe skip or copy content?
                // Let's stick to files for safety, or recursively find textures.
                
                if (Directory.Exists(path))
                {
                    // It's a directory
                    string[] subGuids = AssetDatabase.FindAssets("t:Texture", new[] { path });
                    foreach (var subGuid in subGuids)
                    {
                        CopyAsset(AssetDatabase.GUIDToAssetPath(subGuid), destFolder);
                    }
                }
                else
                {
                    // It's a file
                    CopyAsset(path, destFolder);
                }
            }
            
            AssetDatabase.Refresh();
            ProjectProcessor.FullScan(); // Re-scan to update UI
        }

        private static void CopyAsset(string sourcePath, string destFolder)
        {
            if (string.IsNullOrEmpty(sourcePath)) return;
            
            // Check if it's a texture (rough check)
            Texture importer = AssetDatabase.LoadAssetAtPath<Texture>(sourcePath);
            if (importer == null) return; // Not a texture

            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(destFolder, fileName);
            
            // Auto-rename if exists
            destPath = AssetDatabase.GenerateUniqueAssetPath(destPath);
            
            AssetDatabase.CopyAsset(sourcePath, destPath);
            Debug.Log($"[SharedTexHub] Added to library: {destPath}");
        }
    }
}
