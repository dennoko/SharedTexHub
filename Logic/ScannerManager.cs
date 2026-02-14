using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;
using SharedTexHub.Logic.Scanner;
using System.Collections.Generic;
using System.Linq;

namespace SharedTexHub.Logic
{
    public static class ScannerManager
    {
        private static bool isScanning = false;
        private static IEnumerator<float> scanEnumerator;
        
        public static bool IsScanning => isScanning;
        public static float Progress { get; private set; }
        
        // Configuration
        private const float MAX_TIME_PER_FRAME = 0.01f; // 10ms

        public static void StartFullScan()
        {
            if (isScanning) return;
            
            isScanning = true;
            Progress = 0f;
            scanEnumerator = RunFullScan();
            
            EditorApplication.update += UpdateScan;
        }

        private static void UpdateScan()
        {
            if (!isScanning || scanEnumerator == null)
            {
                StopScan();
                return;
            }

            try
            {
                // Continue execution until it yields or completes
                // The enumerator returns 'progress' (0.0 to 1.0)
                if (scanEnumerator.MoveNext())
                {
                    Progress = scanEnumerator.Current;
                }
                else
                {
                    // Finished
                    StopScan();
                    Debug.Log("[SharedTexHub] Scan Completed Successfully.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SharedTexHub] Scan Failed: {e}");
                StopScan();
            }
        }

        private static void StopScan()
        {
            isScanning = false;
            scanEnumerator = null;
            EditorApplication.update -= UpdateScan;
            EditorUtility.ClearProgressBar();
        }

        private static IEnumerator<float> RunFullScan()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            HashSet<(string, Category)> visitedItems = new HashSet<(string, Category)>();
            
            // Initialization
            // DatabaseManager.Clear(); // REMOVED for incremental scan
            HashGenerator.ClearCache();
            
            yield return 0.05f;

            // 1. Scan Manual Folders
            foreach (Category category in System.Enum.GetValues(typeof(Category)))
            {
                var textures = FolderScanner.Scan(category);
                foreach (var t in textures)
                {
                    DatabaseManager.AddOrUpdate(t);
                    visitedItems.Add((t.guid, t.category));
                }
            }

            yield return 0.1f;

            // 2. Scan Materials
            string[] guids = AssetDatabase.FindAssets("t:Material");
            List<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList();
            
            List<ITextureScanner> scanners = new List<ITextureScanner>
            {
                new MatCapScanner(),
                new TilingScanner(),
                new NormalScanner(),
                new MaskScanner(),
                new DecalScanner()
            };

            int total = paths.Count;
            int current = 0;

            foreach (string path in paths)
            {
                current++;
                
                // Time Check
                if (!stopwatch.IsRunning) stopwatch.Start();
                else if (stopwatch.Elapsed.TotalSeconds > MAX_TIME_PER_FRAME)
                {
                    // Yield control back to Editor
                    EditorUtility.DisplayProgressBar("SharedTexHub", $"Scanning Materials... {current}/{total}", (float)current / total);
                    yield return 0.1f + (0.9f * ((float)current / total));
                    stopwatch.Reset();
                    stopwatch.Start();
                }

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                foreach (var scanner in scanners)
                {
                    foreach (var info in scanner.Scan(mat))
                    {
                        DatabaseManager.AddOrUpdate(info);
                        visitedItems.Add((info.guid, info.category));
                    }
                }
            }
            
            stopwatch.Stop();
            
            // Finalize
            DatabaseManager.CleanupExcept(visitedItems);
            DatabaseManager.Save();
            
            yield return 1.0f;
        }
        
        // For incremental updates (called from AssetPostprocessor)
        public static void ScanSpecificMaterials(string[] paths)
        {
             // If full scan is running, maybe ignore? Or queue?
             // Since this is usually small, we can run it synchronously or start a mini-coroutine.
             // For simplicity, let's just run it synchronously but efficiently.
             
             List<ITextureScanner> scanners = new List<ITextureScanner>
            {
                new MatCapScanner(),
                new TilingScanner(),
                new NormalScanner(),
                new MaskScanner(),
                new DecalScanner()
            };

            // Use 'delayCall' to ensure we are not in the middle of import process if needed, 
            // but PostprocessAllAssets is usually safe.
             
             foreach (string path in paths)
             {
                 Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                 if (mat == null) continue;

                 foreach (var scanner in scanners)
                 {
                     foreach (var info in scanner.Scan(mat))
                     {
                         // Check duplications inside DatabaseManager
                         DatabaseManager.AddOrUpdate(info);
                     }
                 }
             }
             DatabaseManager.Save();
        }
    }
}
