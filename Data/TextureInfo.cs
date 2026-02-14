using System;

namespace SharedTexHub.Data
{
    [Serializable]
    public class TextureInfo
    {
        public string guid;
        public string path;
        public Category category;
        public string hash;
        public UnityEngine.Color[] colorGrid; // 3x3 grid = 9 colors
        public UnityEngine.Vector3 mainHsv; // H, S, V of center color
        public float colorSpread; // Variance of colors
        public long lastWriteTime; // Ticks

        public TextureInfo(string guid, string path, Category category)
        {
            this.guid = guid;
            this.path = path;
            this.category = category;
        }
    }
}
