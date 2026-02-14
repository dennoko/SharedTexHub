using System;

namespace SharedTexHub.Data
{
    [Serializable]
    public class TextureInfo
    {
        public string guid;
        public string path;
        public Category category;

        public TextureInfo(string guid, string path, Category category)
        {
            this.guid = guid;
            this.path = path;
            this.category = category;
        }
    }
}
