using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;
using System.IO;

namespace SharedTexHub.Logic
{
    public static class AssetManager
    {
        private const string LIBRARY_PATH = "Assets/SharedTexHub/Library";

        public static void CopyToLibrary(TextureInfo info)
        {
            if (!Directory.Exists(LIBRARY_PATH))
            {
                Directory.CreateDirectory(LIBRARY_PATH);
                AssetDatabase.Refresh();
            }

            string originalPath = info.path;
            string fileName = Path.GetFileName(originalPath);
            string destinationPath = Path.Combine(LIBRARY_PATH, fileName);
            
            // Unify separators
            destinationPath = destinationPath.Replace("\\", "/");

            if (destinationPath == originalPath)
            {
                Debug.LogWarning("[SharedTexHub] Texture is already in Library.");
                return;
            }

            destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);

            if (AssetDatabase.CopyAsset(originalPath, destinationPath))
            {
                Debug.Log($"[SharedTexHub] Copied {fileName} to {destinationPath}");
                // Ping the new asset
                Object newAsset = AssetDatabase.LoadAssetAtPath<Object>(destinationPath);
                EditorGUIUtility.PingObject(newAsset);
            }
            else
            {
                Debug.LogError($"[SharedTexHub] Failed to copy asset to {destinationPath}");
            }
        }
    }
}
