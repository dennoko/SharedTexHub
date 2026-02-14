using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;

namespace SharedTexHub.Logic.Scanner
{
    public static class FolderScanner
    {
        public static IEnumerable<TextureInfo> Scan(Category category)
        {
            string folderPath = DirectoryManager.GetCategoryFolderPath(category);
            
            // Ensure folder exists before scanning (or just skip if not exists)
            // If we ensure it here, empty folders will be created which might be annoying?
            // Let's check existence first.
            if (!System.IO.Directory.Exists(folderPath))
            {
                yield break;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                yield return new TextureInfo(guid, path, category);
            }
        }
    }
}
