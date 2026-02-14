using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;
using System.Collections.Generic;

namespace SharedTexHub.Logic
{
    public static class ColorAnalyzer
    {
        public static void Analyze(TextureInfo info)
        {
            Texture2D texture = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Texture>(info.path));
            
            // Fallback if AssetPreview is not ready or null
            if (texture == null)
            {
                 // Try MiniThumbnail
                 texture = AssetPreview.GetMiniThumbnail(AssetDatabase.LoadAssetAtPath<Object>(info.path));
            }

            if (texture == null) return;

            // Make sure texture is readable (AssetPreview usually returns a readable copy)
            // But just in case, catch exceptions
            try
            {
                if (!texture.isReadable) return;

                int w = texture.width;
                int h = texture.height;
                Color[] pixels = texture.GetPixels();

                // Determine if we should apply circular mask (only for MatCap category)
                bool applyCircularMask = info.category == Category.MatCap;
                Vector2 centerUV = new Vector2(w * 0.5f, h * 0.5f);
                float radiusSq = (w * 0.5f) * (w * 0.5f);

                // 1. Calculate Main Color (Average of entire masked area)
                info.colorGrid = new Color[9]; // Keep 3x3 for compatibility/display if needed, but we use 4x4 for spread
                Color mainColor = GetAverageColor(pixels, w, 0, 0, w, h, applyCircularMask, centerUV, radiusSq);
                Color.RGBToHSV(mainColor, out float mHue, out float mSat, out float mVal);
                info.mainHsv = new Vector3(mHue, mSat, mVal);


                // 2. Calculate Spread using 4x4 Grid Centers
                // We don't store the 4x4 grid in TextureInfo to save memory, just compute spread
                int split = 4;
                int cellW = w / split;
                int cellH = h / split;
                
                List<Color> subColors = new List<Color>();

                for (int y = 0; y < split; y++)
                {
                    for (int x = 0; x < split; x++)
                    {
                        // Get Center of the cell
                        int centerX = x * cellW + cellW / 2;
                        int centerY = y * cellH + cellH / 2;
                        
                        if (applyCircularMask)
                        {
                            float dx = centerX - centerUV.x;
                            float dy = centerY - centerUV.y;
                            if (dx * dx + dy * dy > radiusSq) continue;
                        }
                        
                        // Sample 3x3 around center to reduce noise, or just single pixel?
                        // Let's take single pixel for speed on small preview, or small 3x3 avg
                        Color subColor = GetAverageColor(pixels, w, centerX - 1, centerY - 1, 3, 3, false, Vector2.zero, 0); 
                        subColors.Add(subColor);
                    }
                }

                if (subColors.Count > 0)
                {
                    float totalDist = 0;
                    foreach (var c in subColors)
                    {
                         // Distance in RGB space
                         float dr = c.r - mainColor.r;
                         float dg = c.g - mainColor.g;
                         float db = c.b - mainColor.b;
                         totalDist += Mathf.Sqrt(dr * dr + dg * dg + db * db);
                    }
                    info.colorSpread = totalDist / subColors.Count;
                }
                else
                {
                    info.colorSpread = 0;
                }
                
                // Legacy 3x3 grid filling for compatibility if needed (center is main)
                // We just fill center with mainColor
                 info.colorGrid[4] = mainColor;

            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SharedTexHub] Failed to analyze color for {info.path}: {e.Message}");
            }
        }

        private static Color GetAverageColor(Color[] pixels, int textureWidth, int startX, int startY, int width, int height, bool applyMask, Vector2 center, float radiusSq)
        {
            float r = 0, g = 0, b = 0;
            int count = 0;
            
            // Clamp
            if (startX < 0) startX = 0;
            if (startY < 0) startY = 0;
            if (startX + width > textureWidth) width = textureWidth - startX;
            int textureHeight = pixels.Length / textureWidth;
            if (startY + height > textureHeight) height = textureHeight - startY;

            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    if (applyMask)
                    {
                        // Check distance from center
                        float dx = x - center.x;
                        float dy = y - center.y;
                        if (dx * dx + dy * dy > radiusSq) continue;
                    }

                    int idx = y * textureWidth + x;
                    if (idx < pixels.Length)
                    {
                        Color c = pixels[idx];
                        r += c.r;
                        g += c.g;
                        b += c.b;
                        count++;
                    }
                }
            }

            if (count == 0) return Color.black;
            return new Color(r / count, g / count, b / count);
        }
    }
}
