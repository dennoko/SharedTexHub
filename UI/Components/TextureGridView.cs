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
        
        private float itemSize = 100f; // Default size

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
            // float itemSize = 100f; // Removed local declaration
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

            // Footer with Scale Slider
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Scale:", GUILayout.Width(40));
            itemSize = GUILayout.HorizontalSlider(itemSize, 50f, 200f, GUILayout.Width(100));
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
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
            
            // Use GUILayout.Box or Label instead of Button to handle events manually
            // Button consumes events which makes Drag detection harder if we want to avoid selection on drag
            GUIStyle textureStyle = new GUIStyle(GUI.skin.box); // Use a box style for the texture background
            textureStyle.alignment = TextAnchor.MiddleCenter;

            if (tex != null)
            {
                // Use AssetPreview to get correct visual for Normal Maps etc.
                Texture preview = AssetPreview.GetAssetPreview(tex);
                if (preview == null) 
                {
                    // Fallback to mini thumbnail if preview is loading or unavailable
                    preview = AssetPreview.GetMiniThumbnail(tex);
                }

                // Draw Texture Preview
                if (preview != null)
                {
                    GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.Box(rect, "Loading...", textureStyle);
                }
                
                // Label
                GUILayout.Label(tex.name, EditorStyles.miniLabel, GUILayout.Width(size));

                // Events
                Event e = Event.current;

                if (rect.Contains(e.mousePosition))
                {
                    // Handle Click (Selection) -> MouseUp
                    if (e.type == EventType.MouseUp && e.button == 0)
                    {
                        EditorGUIUtility.PingObject(tex);
                        Selection.activeObject = tex;
                        e.Use();
                    }
                    // Handle Drag & Drop -> MouseDrag
                    else if (e.type == EventType.MouseDrag)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new Object[] { tex };
                        DragAndDrop.StartDrag("Texture Drag");
                        e.Use();
                    }
                    // Context Menu
                    else if (e.type == EventType.ContextClick)
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Copy to Library"), false, () => 
                        {
                            SharedTexHub.Logic.AssetManager.CopyToLibrary(info);
                        });
                        menu.ShowAsContext();
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
                    return list.OrderBy(t => !IsGrayscale(t.mainHsv.y)) // Grayscale first (IsGrayscale=true comes before false? No, bool sort: False then True. So !IsGrayscale for Grayscale first)
                               // Actually: OrderBy(bool) puts False first, then True.
                               // We want IsGrayscale=true to be first.
                               .OrderByDescending(t => IsGrayscale(t.mainHsv.y)) 
                               .ThenBy(t => IsGrayscale(t.mainHsv.y) ? t.mainHsv.z : QuantizeHue(t.mainHsv.x)) // If Gray: sort by Value. Else: sort by Hue
                               .ThenBy(t => t.mainHsv.y) // Saturation
                               .ThenBy(t => t.mainHsv.z) // Value
                               .ToList();
                case SortOption.ColorSpread:
                    // 1. Grayscale Check
                    // 2. Quantized Spread (10 steps)
                    // 3. Quantized Hue
                    return list.OrderByDescending(t => IsGrayscale(t.mainHsv.y))
                               .ThenBy(t => IsGrayscale(t.mainHsv.y) ? t.mainHsv.z : QuantizeSpread(t.colorSpread, 10)) // If Gray: sort by Value. Else: sort by Spread
                               .ThenBy(t => IsGrayscale(t.mainHsv.y) ? 0 : QuantizeHue(t.mainHsv.x)) // If Gray: ignore Hue. Else: sort by Hue
                               .ToList();
                default:
                    return list;
            }
        }

        private bool IsGrayscale(float saturation)
        {
            return saturation < 0.15f; // Threshold for grayscale
        }

        // Quantize hue to reduce fine-grained sorting noise
        // Hue is 0-1. Let's make 24 steps (every 15 degrees)
        private float QuantizeHue(float hue)
        {
            int steps = 24;
            return Mathf.Floor(hue * steps) / (float)steps;
        }

        private float QuantizeSpread(float spread, int steps)
        {
            // Spread is now Hue distance (0.0 to 0.5)
            // Max possible distance in Hue circle is 0.5
            
            float maxSpread = 0.5f;
            float normalized = Mathf.Clamp01(spread / maxSpread);
            
            return Mathf.Floor(normalized * steps) / (float)steps;
        }
    }
}
