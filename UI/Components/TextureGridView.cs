using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;
using System.Collections.Generic;
using System.Linq;

namespace SharedTexHub.UI.Components
{
    public class TextureGridView
    {
        private Vector2 scrollPosition;
        private string searchString = "";
        private SortOption sortOption = SortOption.Name;

        public enum SortOption
        {
            Name,
            Color,
            ColorSpread
        }
        
        public void Draw(List<TextureInfo> textures)
        {
            // Search Bar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            searchString = GUILayout.TextField(searchString, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            
            // Sort Option
            GUILayout.Label("Sort by:", GUILayout.Width(50));
            sortOption = (SortOption)EditorGUILayout.EnumPopup(sortOption, GUILayout.Width(100));

            GUILayout.EndHorizontal();

            // Filter
            List<TextureInfo> filtered = textures;
            if (!string.IsNullOrEmpty(searchString))
            {
                filtered = textures.Where(t => t.path.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            // Sort
            filtered = SortTextures(filtered);

            if (filtered.Count == 0)
            {
                GUILayout.Label("No textures found.");
                return;
            }

            // Web-style Grid
            float windowWidth = EditorGUIUtility.currentViewWidth;
            float padding = 10f;
            float itemSize = 100f;
            float spacing = 5f;
            int columns = Mathf.Max(1, (int)((windowWidth - padding * 2) / (itemSize + spacing)));
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            // Fix: Create a style with padding
            GUIStyle paddingStyle = new GUIStyle();
            paddingStyle.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            GUILayout.BeginVertical(paddingStyle);
            
            for (int i = 0; i < filtered.Count; i += columns)
            {
                GUILayout.BeginHorizontal();
                for (int j = 0; j < columns; j++)
                {
                    int index = i + j;
                    if (index >= filtered.Count)
                    {
                         GUILayout.Label("", GUILayout.Width(itemSize)); // Spacer
                         continue;
                    }
                    
                    var info = filtered[index];
                    DrawTextureItem(info, itemSize);
                    
                    if (j < columns - 1) GUILayout.Space(spacing);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(spacing);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void DrawTextureItem(TextureInfo info, float size)
        {
            // Get Texture (cached preview)
            // AssetDatabase.LoadAssetAtPath is reasonably fast for Editor usage?
            // If it's slow, we might need a separate cache.
            // But Texture itself is an asset.
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(info.path);
            
            GUILayout.BeginVertical(GUILayout.Width(size), GUILayout.Height(size + 20));
            
            Rect rect = GUILayoutUtility.GetRect(size, size);
            
            if (tex != null)
            {
                // Use AssetPreview to get correct visual for Normal Maps etc.
                Texture preview = AssetPreview.GetAssetPreview(tex);
                if (preview == null) 
                {
                    // Fallback to mini thumbnail if preview is loading or unavailable
                    preview = AssetPreview.GetMiniThumbnail(tex);
                }

                if (preview != null)
                {
                    GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.Box(rect, "Loading...");
                }
                
                // Label
                GUILayout.Label(tex.name, EditorStyles.miniLabel, GUILayout.Width(size));

                // Events
                Event e = Event.current;
                if (rect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.ContextClick)
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Copy to Library"), false, () => 
                        {
                            SharedTexHub.Logic.AssetManager.CopyToLibrary(info);
                        });
                        menu.ShowAsContext();
                        e.Use();
                    }
                    else if (e.type == EventType.MouseDown)
                    {
                        // Ping
                        EditorGUIUtility.PingObject(tex);
                        Selection.activeObject = tex;
                        e.Use();
                    }
                    else if (e.type == EventType.MouseDrag)
                    {
                        // Drag and Drop
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new Object[] { tex };
                        DragAndDrop.StartDrag(tex.name);
                        e.Use();
                    }
                }
            }
            else
            {
                // Missing texture?
                GUI.Box(rect, "Missing");
                GUILayout.Label("Missing", EditorStyles.miniLabel, GUILayout.Width(size));
            }
            
            GUILayout.EndVertical();
        }
        private List<TextureInfo> SortTextures(List<TextureInfo> list)
        {
            switch (sortOption)
            {
                case SortOption.Name:
                    return list.OrderBy(t => t.path).ToList();
                case SortOption.Color:
                    return list.OrderBy(t => t.mainHsv.x) // Hue
                               .ThenBy(t => t.mainHsv.y) // Saturation
                               .ThenBy(t => t.mainHsv.z) // Value
                               .ToList();
                case SortOption.ColorSpread:
                    return list.OrderBy(t => t.mainHsv.x) // Hue
                               .ThenBy(t => t.colorSpread) // Spread (Low to High? or High to Low? let's go Low to High for "clean" to "dirty")
                               .ToList();
                default:
                    return list;
            }
        }
    }
}
