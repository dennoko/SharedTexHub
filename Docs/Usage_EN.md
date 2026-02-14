# SharedTexHub User Guide

SharedTexHub is a Unity extension designed to help you organize and access textures within your project, making it easier to reuse assets like MatCaps and Tiling textures during VRChat avatar creation.

## Basic Usage

### 1. Opening the Window
Go to **Tools > SharedTexHub > Open Window** in the menu bar.

### 2. Automatic Texture Detection
When opened, the tool automatically scans all materials in your project (currently supporting lilToon) and categorizes used textures into the following tabs:

*   **MatCap**: MatCap textures
*   **Tiling**: Tiling textures (Noise, Fabric, patterns, etc.)
*   **Normal**: Normal maps
*   **Mask**: Mask textures
*   **Decal**: Decal textures

**Note:** Duplicate textures found in multiple materials will be grouped together.

### 3. Adjusting the View
*   **Search**: Use the search bar at the top left to filter by name.
*   **Sort by**: Change the sorting order using the dropdown at the top right.
    *   **Name**: Alphabetical order.
    *   **Color**: Sort by color variance and hue.
*   **Scale**: Use the slider at the bottom right to adjust the thumbnail size.

### 4. Applying to Materials
You can drag and drop textures directly from the list to other materials.
*   **Drag** a texture from the list and **Drop** it onto any texture slot in the Inspector or onto an object in the Scene view.

## Manual Registration

You can manually add textures to the library even if they are not used in any material.

Method 1: Context Menu
1.  Select a texture (or folder) in the Project view.
2.  Right-click and select **SharedTexHub > Add to Library > [Category]**.
3.  The texture will be copied to the library folder and added to SharedTexHub.

Method 2: Direct Folder Access
1.  Click the **Open Folder** button at the bottom left of the SharedTexHub window.
2.  This opens the storage folder for the current category in your file explorer.
3.  Copy or save texture files directly into this folder.
4.  They will be automatically detected when you return to Unity.

## Other Features
*   **Rescan**: If updated textures are not appearing, click the **Force Scan** button (search icon) to refresh the database.
*   **Context Menu**: Right-click on any texture in the list for options:
    *   **Copy to Library**: Copies the texture to the SharedTexHub library folder.
    *   **Ignore**: Hide this texture from the list (useful for false positives).

### Ignore List
You can hide textures that are incorrectly detected or not needed.
1.  **Right-click > Ignore** on a texture to hide it.
2.  To restore it, toggle the **"Show Ignored"** button in the toolbar.
3.  Right-click the hidden texture and select **Restore**.

