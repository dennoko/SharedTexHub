using UnityEngine;
using UnityEditor;
using SharedTexHub.UI.Tabs;
using SharedTexHub.Logic;
using System.Collections.Generic;

namespace SharedTexHub.UI
{
    public class SharedTexHubWindow : EditorWindow
    {
        [MenuItem("dennokoworks/TextureHub")]
        public static void ShowWindow()
        {
            GetWindow<SharedTexHubWindow>("SharedTexHub");
        }

        private MatCapTab matCapTab;
        private TilingTab tilingTab;
        private NormalTab normalTab;
        private MaskTab maskTab;
        private DecalTab decalTab;
        
        private int selectedTab = 0;
        private string[] tabNames = new string[] { "MatCap", "Tiling", "Normal", "Mask", "Decal" };
        private ITabView[] tabs;

        private void OnEnable()
        {
            matCapTab = new MatCapTab();
            tilingTab = new TilingTab();
            normalTab = new NormalTab();
            maskTab = new MaskTab();
            decalTab = new DecalTab();

            tabs = new ITabView[] { matCapTab, tilingTab, normalTab, maskTab, decalTab };
            
            foreach(var tab in tabs) tab.OnEnable();
            
            SharedTexHub.Logic.ScannerManager.OnProgressChanged += Repaint;
        }

        private void OnDisable()
        {
            SharedTexHub.Logic.ScannerManager.OnProgressChanged -= Repaint;
            
            if (tabs != null)
            {
                foreach(var tab in tabs) tab.OnDisable();
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, EditorStyles.toolbarButton);
            

            // Force Scan Button / Cancel Button
            GUILayout.FlexibleSpace();
            if (SharedTexHub.Logic.ScannerManager.IsScanning)
            {
                if (GUILayout.Button("Cancel", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SharedTexHub.Logic.ScannerManager.StopScan();
                }
            }
            else
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    // Force rebuild if Debug Mode is active
                    SharedTexHub.Logic.ScannerManager.StartFullScan(SharedTexHub.UI.Components.TextureGridView.ShowDebugColor);
                }
            }
            GUILayout.EndHorizontal();

            // Progress Bar
            if (SharedTexHub.Logic.ScannerManager.IsScanning)
            {
                Rect rect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(rect, SharedTexHub.Logic.ScannerManager.Progress, $"Scanning... {(int)(SharedTexHub.Logic.ScannerManager.Progress * 100)}%");
            }

            if (tabs != null && selectedTab >= 0 && selectedTab < tabs.Length)
            {
                tabs[selectedTab].Draw();
            }
        }
    }
}
