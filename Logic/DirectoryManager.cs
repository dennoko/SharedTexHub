using System.IO;
using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;

namespace SharedTexHub.Logic
{
    public static class DirectoryManager
    {
        private const string RootPath = "Assets/SharedTexHub";

        public static string GetCategoryFolderPath(Category category)
        {
            return Path.Combine(RootPath, category.ToString());
        }

        public static void EnsureCategoryFolder(Category category)
        {
            string path = GetCategoryFolderPath(category);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        public static void OpenCategoryFolder(Category category)
        {
            EnsureCategoryFolder(category);
            string path = GetCategoryFolderPath(category);
            
            // Convert to absolute system path
            string absPath = Path.GetFullPath(path);
            EditorUtility.RevealInFinder(absPath);
        }
    }
}
