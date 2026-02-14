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

        // Cache for filtered and sorted list
        private List<TextureInfo> cachedList = null;
        private List<TextureInfo> lastSourceList = null; // Track reference
        private string lastSearchString = "";
        private SortOption lastSortOption = SortOption.Name;
        private int lastRawListCount = -1;

        public void Draw(List<TextureInfo> textures, Category currentCategory)
        {
            // Search Bar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            string newSearchString = GUILayout.TextField(searchString, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            if (newSearchString != searchString)
            {
                searchString = newSearchString;
                cachedList = null; // Invalidate cache
            }
            GUILayout.FlexibleSpace();
            
            // Sort Option
            GUILayout.Label("Sort by:", GUILayout.Width(50));
            SortOption newSortOption = (SortOption)EditorGUILayout.EnumPopup(sortOption, GUILayout.Width(100));
            if (newSortOption != sortOption)
            {
                sortOption = newSortOption;
                cachedList = null; // Invalidate cache
            }

            GUILayout.EndHorizontal();

            // Refresh cache if needed
            // Check reference equality first (fastest), then count
            if (cachedList == null || textures != lastSourceList || textures.Count != lastRawListCount)
            {
                UpdateCache(textures);
            }

            if (cachedList.Count == 0)
            {
                 // Draw footer even if empty
            }

            // Web-style Grid
            float windowWidth = EditorGUIUtility.currentViewWidth;
            float padding = 10f;
            float spacing = 5f;
            float itemHeight = itemSize + 20f;
            int columns = Mathf.Max(1, (int)((windowWidth - padding * 2) / (itemSize + spacing)));
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            GUIStyle paddingStyle = new GUIStyle();
            paddingStyle.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            GUILayout.BeginVertical(paddingStyle);

            if (cachedList.Count > 0)
            {
                int totalRows = Mathf.CeilToInt((float)cachedList.Count / columns);
                float totalHeight = totalRows * (itemHeight + spacing);

                // Virtualization Logic
                // Approximate view height (can use fixed value or calculate)
                // Since we are inside a ScrollView, we can use scrollPosition to determine visible range.
                // Assuming standard window height approx 800, let's buffer a bit.
                // Better: Use GUILayoutUtility.GetLastRect() from previous frame? Or just Screen.height
                float viewHeight = Screen.height; 
                
                int firstVisibleRow = Mathf.FloorToInt(scrollPosition.y / (itemHeight + spacing));
                int visibleRowCount = Mathf.CeilToInt(viewHeight / (itemHeight + spacing)) + 2; // Buffer
                
                firstVisibleRow = Mathf.Max(0, firstVisibleRow);
                int lastVisibleRow = Mathf.Min(totalRows - 1, firstVisibleRow + visibleRowCount);

                // Top Spacer
                float topSpaceHeight = firstVisibleRow * (itemHeight + spacing);
                if (topSpaceHeight > 0) GUILayout.Space(topSpaceHeight);

                // Draw Visible Rows
                for (int row = firstVisibleRow; row <= lastVisibleRow; row++)
                {
                    GUILayout.BeginHorizontal();
                    for (int col = 0; col < columns; col++)
                    {
                        int index = row * columns + col;
                        if (index >= cachedList.Count)
                        {
                             GUILayout.Label("", GUILayout.Width(itemSize)); // Spacer
                             continue;
                        }
                        
                        var info = cachedList[index];
                        DrawTextureItem(info, itemSize);
                        
                        if (col < columns - 1) GUILayout.Space(spacing);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(spacing);
                }

                // Bottom Spacer
                int remainingRows = totalRows - 1 - lastVisibleRow;
                float bottomSpaceHeight = remainingRows * (itemHeight + spacing);
                if (bottomSpaceHeight > 0) GUILayout.Space(bottomSpaceHeight);
            }
            else
            {
                GUILayout.Label("No textures found.");
            }
            
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            // Footer with Scale Slider
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Open Folder Button
            if (GUILayout.Button("Open Folder", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                SharedTexHub.Logic.DirectoryManager.OpenCategoryFolder(currentCategory);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Scale:", GUILayout.Width(40));
            float newItemSize = GUILayout.HorizontalSlider(itemSize, 50f, 200f, GUILayout.Width(100));
            if (Mathf.Abs(newItemSize - itemSize) > 1f)
            {
                itemSize = newItemSize;
                // Repaint will happen naturally
            }

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }

        private void UpdateCache(List<TextureInfo> sourceList)
        {
            // Filter
            IEnumerable<TextureInfo> filtered = sourceList;
            if (!string.IsNullOrEmpty(searchString))
            {
                filtered = sourceList.Where(t => t.path.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // Sort
            filtered = SortTextures(filtered);
            
            cachedList = filtered.ToList();
            lastSearchString = searchString;
            lastSortOption = sortOption;
            lastSourceList = sourceList;
            lastRawListCount = sourceList.Count;
        }

        private IEnumerable<TextureInfo> SortTextures(IEnumerable<TextureInfo> list)
        {
            switch (sortOption)
            {
                case SortOption.Name:
                    return list.OrderBy(t => t.path);
                case SortOption.Color:
                    return list.OrderBy(t => !IsGrayscale(t.mainHsv.y)) 
                               .ThenBy(t => IsGrayscale(t.mainHsv.y) ? t.mainHsv.z : QuantizeHue(t.mainHsv.x)) 
                               .ThenBy(t => t.mainHsv.y) 
                               .ThenBy(t => t.mainHsv.z);
                case SortOption.ColorSpread:
                    return list.OrderBy(t => !IsGrayscale(t.mainHsv.y))
                               .ThenBy(t => IsGrayscale(t.mainHsv.y) ? t.mainHsv.z : QuantizeSpread(t.colorSpread, 10)) 
                               .ThenBy(t => IsGrayscale(t.mainHsv.y) ? 0 : QuantizeHue(t.mainHsv.x));
                default:
                    return list;
            }
        }

        private void DrawTextureItem(TextureInfo info, float size)
        {
            // Get Texture (cached preview)
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(info.path);
            
            GUILayout.BeginVertical(GUILayout.Width(size), GUILayout.Height(size + 20));
            
            Rect rect = GUILayoutUtility.GetRect(size, size);
            
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
                int controlID = GUIUtility.GetControlID(FocusType.Passive);

                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (rect.Contains(e.mousePosition) && e.button == 0)
                        {
                            GUIUtility.hotControl = controlID;
                            e.Use();
                        }
                        break;

                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlID && e.button == 0)
                        {
                            GUIUtility.hotControl = 0; // Release control
                            
                            if (rect.Contains(e.mousePosition))
                            {
                                EditorGUIUtility.PingObject(tex);
                                Selection.activeObject = tex;
                            }
                            e.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlID)
                        {
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.objectReferences = new Object[] { tex };
                            DragAndDrop.StartDrag("Texture Drag");
                            
                            GUIUtility.hotControl = 0; 
                            e.Use();
                        }
                        break;

                    case EventType.ContextClick:
                         if (rect.Contains(e.mousePosition))
                         {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Copy to Library"), false, () => 
                            {
                                SharedTexHub.Logic.AssetManager.CopyToLibrary(info);
                            });
                            menu.ShowAsContext();
                            e.Use();
                         }
                         break;
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

        private bool IsGrayscale(float saturation)
        {
            return saturation < 0.15f; 
        }

        private float QuantizeHue(float hue)
        {
            int steps = 24;
            return Mathf.Floor(hue * steps) / (float)steps;
        }

        private float QuantizeSpread(float spread, int steps)
        {
            float maxSpread = 0.5f;
            float normalized = Mathf.Clamp01(spread / maxSpread);
            
            return Mathf.Floor(normalized * steps) / (float)steps;
        }
    }
}
