using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;
using SharedTexHub.Logic;
using SharedTexHub.UI.Components;
using System.Collections.Generic;
using System.Linq;

namespace SharedTexHub.UI.Tabs
{
    public abstract class TextureTabBase : ITabView
    {
        protected TextureGridView gridView;
        protected List<TextureInfo> cachedTextures;
        protected abstract Category TargetCategory { get; }

        public TextureTabBase()
        {
            gridView = new TextureGridView();
            cachedTextures = new List<TextureInfo>();
        }

        public void OnEnable()
        {
            DatabaseManager.OnDatabaseUpdated += UpdateList;
            UpdateList();
        }

        public void OnDisable()
        {
            DatabaseManager.OnDatabaseUpdated -= UpdateList;
        }

        public void Draw()
        {
            // Initial load if needed
            if (cachedTextures == null || (cachedTextures.Count == 0 && DatabaseManager.Database.textures.Count > 0 && !DatabaseManager.Database.textures.Any(t => t.category == TargetCategory)))
            { 
               // logic above is flawed. UpdateList logic is safer.
            }
            // Just rely on OnEnable or check null
            if (cachedTextures == null)
            {
                UpdateList();
            }

            gridView.Draw(cachedTextures, TargetCategory);
        }

        private void UpdateList()
        {
            cachedTextures = DatabaseManager.Database.textures
                .Where(t => t.category == TargetCategory)
                .GroupBy(t => t.hash) // Group by hash
                .Select(g => g.First()) // Select the first one from each group
                .ToList();
        }
    }
}
