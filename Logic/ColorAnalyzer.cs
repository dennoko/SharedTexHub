using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharedTexHub.Logic
{
    public static class ColorAnalyzer
    {
        // Async version of Analyze
        public static async Task AnalyzeAsync(TextureInfo info)
        {
            Texture2D texture = null;
            bool isTempTexture = false;

            // 1. Texture Loading (Must be on Main Thread)
            try
            {
                texture = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Texture>(info.path));
                
                // Fallback if AssetPreview is not ready, null, or not readable
                if (texture == null || !texture.isReadable)
                {
                    texture = AssetPreview.GetMiniThumbnail(AssetDatabase.LoadAssetAtPath<Object>(info.path));
                }

                // If still null or not readable, try loading from disk
                if (texture == null || !texture.isReadable)
                {
                    if (System.IO.File.Exists(info.path))
                    {
                        try 
                        {
                            byte[] bytes = await Task.Run(() => System.IO.File.ReadAllBytes(info.path));
                            texture = new Texture2D(2, 2);
                            if (texture.LoadImage(bytes))
                            {
                                isTempTexture = true;
                            }
                            else
                            {
                                Object.DestroyImmediate(texture);
                                texture = null;
                            }
                        }
                        catch
                        {
                            if (texture != null) Object.DestroyImmediate(texture);
                            texture = null;
                        }
                    }
                }

                if (texture == null) return;
                if (!texture.isReadable)
                {
                    if (isTempTexture) Object.DestroyImmediate(texture);
                    return;
                }

                int w = texture.width;
                int h = texture.height;
                Color32[] pixels = texture.GetPixels32(); // Use Color32 for performance and thread safety
                
                // Cleanup texture if temp
                if (isTempTexture)
                {
                    Object.DestroyImmediate(texture);
                }

                // 2. Analysis (Background Thread)
                await Task.Run(() => 
                {
                   ProcessPixels(info, pixels, w, h);
                });

            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SharedTexHub] Failed to analyze color for {info.path}: {e.Message}");
                if (isTempTexture && texture != null) Object.DestroyImmediate(texture);
            }
        }

        // Keep synchronous method for compatibility, but it just waits (not recommended for main thread loop)
        public static void Analyze(TextureInfo info)
        {
             // This is a blocking call, use with caution or only for single item updates
             var task = AnalyzeAsync(info);
             task.Wait(); 
        }

        private static void ProcessPixels(TextureInfo info, Color32[] pixels, int w, int h)
        {
            // Determine if we should apply circular mask (only for MatCap category)
            bool applyCircularMask = info.category == Category.MatCap;
            Vector2 centerUV = new Vector2(w * 0.5f, h * 0.5f);
            float radiusSq = (w * 0.5f) * (w * 0.5f);

            // 1. Calculate Main Color (Average of entire masked area)
            // info.colorGrid is not strictly needed for this version but we initialize it
            info.colorGrid = new Color[9]; 
            Color mainColor = GetAverageColor(pixels, w, 0, 0, w, h, applyCircularMask, centerUV, radiusSq);
            Color.RGBToHSV(mainColor, out float mHue, out float mSat, out float mVal);
            info.mainHsv = new Vector3(mHue, mSat, mVal);


            // 2. Calculate Spread using 4x4 Grid Centers
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
                    
                    // Sample small area around center
                    Color subColor = GetAverageColor(pixels, w, centerX - 1, centerY - 1, 3, 3, false, Vector2.zero, 0); 
                    subColors.Add(subColor);
                }
            }

            if (subColors.Count > 0)
            {
                float totalDist = 0;
                float mainHue = info.mainHsv.x;

                foreach (var c in subColors)
                {
                        // Distance in Hue space (0-1 circular)
                        Color.RGBToHSV(c, out float cHue, out float cSat, out float cVal);
                        
                        // Calculate shortest angular distance
                        float diff = Mathf.Abs(cHue - mainHue);
                        if (diff > 0.5f) diff = 1.0f - diff;
                        
                        totalDist += diff;
                }
                info.colorSpread = totalDist / subColors.Count;
            }
            else
            {
                info.colorSpread = 0;
            }
            
            // Fill center with mainColor
            info.colorGrid[4] = mainColor;
        }

        private static Color GetAverageColor(Color32[] pixels, int textureWidth, int startX, int startY, int width, int height, bool applyMask, Vector2 center, float radiusSq)
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
                        Color32 c = pixels[idx];
                        r += c.r;
                        g += c.g;
                        b += c.b;
                        count++;
                    }
                }
            }

            if (count == 0) return Color.black;
            // Native Color32 is 0-255, Unity Color is 0-1
            return new Color((r / count) / 255f, (g / count) / 255f, (b / count) / 255f);
        }
    }
}

