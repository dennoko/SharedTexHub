using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;
using SharedTexHub.Logic.Scanner;
using System.Collections.Generic;

namespace SharedTexHub.Logic
{
    public class ProjectProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool materialChanged = false;
            foreach (string str in importedAssets)
            {
                if (str.EndsWith(".mat")) { materialChanged = true; break; }
            }
            
            // Re-scan if materials are deleted too, to remove invalid entries.
            // But full scan is heavy. Maybe just on import for now.
            
            if (materialChanged)
            {
                 EditorApplication.delayCall += () => FullScan();
            }
        }

        [MenuItem("dennokoworks/Force Scan")]
        public static void FullScan()
        {
            DatabaseManager.Clear();
            HashGenerator.ClearCache();
            
            // 2. Scan Manual Folders
            foreach (Category category in System.Enum.GetValues(typeof(Category)))
            {
                var textures = Scanner.FolderScanner.Scan(category);
                foreach (var t in textures)
                {
                    DatabaseManager.AddOrUpdate(t);
                }
            }

            AssetDatabase.SaveAssets();
            string[] guids = AssetDatabase.FindAssets("t:Material");
            List<ITextureScanner> scanners = new List<ITextureScanner>
            {
                new MatCapScanner(),
                new TilingScanner(),
                new NormalScanner(),
                new MaskScanner(),
                new DecalScanner()
            };

            int count = 0;
            int total = guids.Length;

            foreach (string guid in guids)
            {
                // Simple progress bar
                if (count % 10 == 0)
                {
                    EditorUtility.DisplayProgressBar("SharedTexHub", "Scanning Materials...", (float)count / total);
                }
                count++;

                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                foreach (var scanner in scanners)
                {
                    foreach (var info in scanner.Scan(mat))
                    {
                        DatabaseManager.AddOrUpdate(info);
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();
            DatabaseManager.Save();
            Debug.Log($"[SharedTexHub] Scan Complete. Found {DatabaseManager.Database.textures.Count} textures.");
        }
    }
}
