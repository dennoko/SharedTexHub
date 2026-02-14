using System.Collections.Generic;
using UnityEngine;

namespace SharedTexHub.Data
{
    public class TextureDatabase : ScriptableObject
    {
        public List<TextureInfo> textures = new List<TextureInfo>();
        public List<string> ignoredGuids = new List<string>();

        public void Clear()
        {
            textures.Clear();
        }

        public void Add(TextureInfo info)
        {
            textures.Add(info);
        }
    }
}
