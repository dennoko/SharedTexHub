using UnityEngine;
using UnityEditor;
using SharedTexHub.Data;
using SharedTexHub.Logic;

public class TestColorAnalysis : MonoBehaviour
{
    [MenuItem("SharedTexHub/Debug/Test Sample Color")]
    public static void TestSample()
    {
        string path = "Assets/Editor/SharedTexHub/Resource/Sample/matcap.png";
        TextureInfo info = new TextureInfo("debug_guid", path, Category.MatCap);
        
        Debug.Log($"[Test] Analyzing {path}...");
        
        ColorAnalyzer.Analyze(info);
        
        Debug.Log($"[Test] Result:");
        Debug.Log($"H: {info.mainHsv.x}, S: {info.mainHsv.y}, V: {info.mainHsv.z}");
        Debug.Log($"Spread: {info.colorSpread}");
        
        Texture2D texture = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Texture>(path));
        if (texture == null) Debug.LogWarning("[Test] AssetPreview returned null");
        else 
        {
            Debug.Log($"[Test] Preview size: {texture.width}x{texture.height}, format: {texture.format}, readable: {texture.isReadable}");
            if (texture.isReadable)
            {
               var pixels = texture.GetPixels();
               Debug.Log($"[Test] Pixel[0]: {pixels[0]}");
               Debug.Log($"[Test] Pixel[Center]: {pixels[pixels.Length/2]}");
            }
        }

        Texture2D mini = AssetPreview.GetMiniThumbnail(AssetDatabase.LoadAssetAtPath<Object>(path));
        if (mini == null) Debug.LogWarning("[Test] MiniThumbnail returned null");
        else Debug.Log($"[Test] MiniThumbnail size: {mini.width}x{mini.height}");
    }
}
