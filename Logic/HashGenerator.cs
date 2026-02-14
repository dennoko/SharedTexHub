using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace SharedTexHub.Logic
{
    public static class HashGenerator
    {
        // Simple cache to avoid re-hashing the same file multiple times during a single session/scan
        private static Dictionary<string, string> _cache = new Dictionary<string, string>();

        public static void ClearCache()
        {
            _cache.Clear();
        }

        public static string GetHash(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return string.Empty;

            if (_cache.TryGetValue(filePath, out string cachedHash))
            {
                return cachedHash;
            }

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    string hash = sb.ToString();
                    _cache[filePath] = hash;
                    return hash;
                }
            }
        }
    }
}
