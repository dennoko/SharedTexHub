using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;

namespace SharedTexHub.Logic
{
    public class ProjectProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Check if any Material was imported
            bool materialChanged = false;
            foreach (string str in importedAssets)
            {
                if (str.EndsWith(".mat")) { materialChanged = true; break; }
            }
            
            // For now, we only trigger incremental scan for imported materials
            // In future we might want to handle deleted assets (remove from DB)
            
            if (materialChanged)
            {
                 // Use DelayCall to avoid blocking import or conflicts
                 // Since we don't want to run full scan on every material save, we might want to debounce or use incremental
                 // For now, let's just trigger a full scan? No, that's heavy.
                 // Let's implement incremental scan in ScannerManager or just call ScanSpecificMaterials
                 
                 // Get only materials
                 var materialPaths = System.Array.FindAll(importedAssets, p => p.EndsWith(".mat"));
                 if (materialPaths.Length > 0)
                 {
                     EditorApplication.delayCall += () => ScannerManager.ScanSpecificMaterials(materialPaths);
                 }
            }
        }

        [MenuItem("dennokoworks/Force Scan")]
        public static void FullScan()
        {
            ScannerManager.StartFullScan();
        }
    }
}
