using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;
using SharedTexHub.Logic.Scanner;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace SharedTexHub.Logic
{
    public static class ScannerManager
    {
        private static bool isScanning = false;
        private static CancellationTokenSource cancelSource;
        
        public static bool IsScanning => isScanning;
        public static float Progress { get; private set; }
        
        // Configuration
        private const float MAX_TIME_PER_FRAME_MS = 10f; // 10ms budget per frame
        
        public static async void StartFullScan(bool forceRebuild = false)
        {
            if (isScanning) return;
            
            isScanning = true;
            Progress = 0f;
            cancelSource = new CancellationTokenSource();
            
            try
            {
                await RunFullScan(forceRebuild, cancelSource.Token);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SharedTexHub] Scan error: {e}");
            }
            finally
            {
                StopScan();
            }
        }

        public static void StopScan()
        {
            if (cancelSource != null)
            {
                cancelSource.Cancel();
                cancelSource.Dispose();
                cancelSource = null;
            }
            isScanning = false;
            EditorUtility.ClearProgressBar();
        }

        public static event System.Action OnProgressChanged;

        private static async Task RunFullScan(bool forceRebuild, CancellationToken token)
        {
            System.Diagnostics.Stopwatch totalWatch = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Stopwatch frameWatch = System.Diagnostics.Stopwatch.StartNew();

            HashSet<(string, Category)> visitedItems = new HashSet<(string, Category)>();
            
            // Initialization
            if (forceRebuild)
            {
                DatabaseManager.Clear();
                DatabaseManager.Save(); 
                Debug.Log("[SharedTexHub] Force Rebuild: Database cleared.");
            }

            HashGenerator.ClearCache();
            
            await Task.Yield(); // Let UI update

            // 1. Scan Manual Folders
            foreach (Category category in System.Enum.GetValues(typeof(Category)))
            {
                if (token.IsCancellationRequested) return;

                var textures = FolderScanner.Scan(category); // This is main thread but fast (just finding assets)
                foreach (var t in textures)
                {
                    await DatabaseManager.AddOrUpdateAsync(t); // Async analysis
                    visitedItems.Add((t.guid, t.category));

                    if (frameWatch.ElapsedMilliseconds > MAX_TIME_PER_FRAME_MS)
                    {
                        await Task.Yield();
                        frameWatch.Restart();
                        OnProgressChanged?.Invoke();
                    }
                }
            }

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
                if (token.IsCancellationRequested) return;
                current++;
                Progress = (float)current / total;
                
                // Frame Budget Check
                if (frameWatch.ElapsedMilliseconds > MAX_TIME_PER_FRAME_MS)
                {
                    await Task.Yield();
                    frameWatch.Restart();
                    OnProgressChanged?.Invoke();
                }

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;


                foreach (var scanner in scanners)
                {
                    foreach (var info in scanner.Scan(mat))
                    {
                        await DatabaseManager.AddOrUpdateAsync(info); // This awaits the background analysis
                        visitedItems.Add((info.guid, info.category));
                    }
                }
            }
            
            totalWatch.Stop();
            Debug.Log($"[SharedTexHub] Scan Completed in {totalWatch.Elapsed.TotalSeconds:F2}s");
            
            // Finalize
            DatabaseManager.CleanupExcept(visitedItems);
            DatabaseManager.Save();
            
            EditorUtility.ClearProgressBar();
        }
        
        // For incremental updates (called from AssetPostprocessor)
        public static async void ScanSpecificMaterials(string[] paths)
        {
             // Fire and forget
             List<ITextureScanner> scanners = new List<ITextureScanner>
            {
                new MatCapScanner(),
                new TilingScanner(),
                new NormalScanner(),
                new MaskScanner(),
                new DecalScanner()
            };

             foreach (string path in paths)
             {
                 Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                 if (mat == null) continue;

                 foreach (var scanner in scanners)
                 {
                     foreach (var info in scanner.Scan(mat))
                     {
                         await DatabaseManager.AddOrUpdateAsync(info);
                     }
                 }
             }
             DatabaseManager.Save();
        }
    }
}
