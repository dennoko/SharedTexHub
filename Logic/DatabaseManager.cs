using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;
using System.Linq;
using System.IO;

namespace SharedTexHub.Logic
{
    public static class DatabaseManager
    {
        private static TextureDatabase _database;
        public static TextureDatabase Database
        {
            get
            {
                if (_database == null) LoadDatabase();
                return _database;
            }
        }

        private const string DATABASE_PATH = "Assets/Editor/SharedTexHub/Data/SharedTexHubDatabase.asset";

        public static void LoadDatabase()
        {
            _database = AssetDatabase.LoadAssetAtPath<TextureDatabase>(DATABASE_PATH);
            if (_database == null)
            {
                _database = ScriptableObject.CreateInstance<TextureDatabase>();
                string directory = Path.GetDirectoryName(DATABASE_PATH);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                AssetDatabase.CreateAsset(_database, DATABASE_PATH);
                AssetDatabase.SaveAssets();
            }
        }

        public static event System.Action OnDatabaseUpdated;

        public static void Save()
        {
            if (_database != null)
            {
                EditorUtility.SetDirty(_database);
                AssetDatabase.SaveAssets();
                OnDatabaseUpdated?.Invoke();
            }
        }

        public static void Clear()
        {
            Database.Clear();
        }

        public static void AddOrUpdate(TextureInfo info)
        {
            if (string.IsNullOrEmpty(info.path) || !File.Exists(info.path)) return;

            long currentTicks = File.GetLastWriteTime(info.path).Ticks;
            var existing = Database.textures.FirstOrDefault(t => t.guid == info.guid && t.category == info.category);

            if (existing != null)
            {
                // Update existing if modified
                if (existing.lastWriteTime != currentTicks)
                {
                    existing.path = info.path;
                    existing.hash = HashGenerator.GetHash(info.path);
                    ColorAnalyzer.Analyze(existing);
                    existing.lastWriteTime = currentTicks;
                    // Debug.Log($"[SharedTexHub] Updated: {info.path}");
                }
                else
                {
                    // Just update path if moved, but skip heavy analysis
                    existing.path = info.path; 
                }
            }
            else
            {
                // New entry
                info.lastWriteTime = currentTicks;
                if (string.IsNullOrEmpty(info.hash))
                {
                    info.hash = HashGenerator.GetHash(info.path);
                }
                
                if (info.colorGrid == null || info.colorGrid.Length == 0)
                {
                    ColorAnalyzer.Analyze(info);
                }

                Database.Add(info);
                // Debug.Log($"[SharedTexHub] Added: {info.path}");
            }
        }

        public static void CleanupExcept(System.Collections.Generic.HashSet<(string guid, Category category)> validItems)
        {
            int removedCount = Database.textures.RemoveAll(t => !validItems.Contains((t.guid, t.category)));
            if (removedCount > 0)
            {
                Debug.Log($"[SharedTexHub] Removed {removedCount} obsolete items.");
            }
        }

        public static void AddIgnore(string guid)
        {
            if (!Database.ignoredGuids.Contains(guid))
            {
                Database.ignoredGuids.Add(guid);
                Save();
            }
        }

        public static void RemoveIgnore(string guid)
        {
            if (Database.ignoredGuids.Remove(guid))
            {
                Save();
            }
        }

        public static bool IsIgnored(string guid)
        {
            return Database.ignoredGuids.Contains(guid);
        }
    }
}
