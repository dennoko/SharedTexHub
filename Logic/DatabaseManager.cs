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
            // Avoid duplicates based on GUID and Category
            if (!Database.textures.Any(t => t.guid == info.guid && t.category == info.category))
            {
                // Compute Hash if needed
                if (string.IsNullOrEmpty(info.hash))
                {
                    info.hash = HashGenerator.GetHash(info.path);
                }

                // Analyze Color if needed
                if (info.colorGrid == null || info.colorGrid.Length == 0)
                {
                    ColorAnalyzer.Analyze(info);
                }

                Database.Add(info);
            }
        }
    }
}
