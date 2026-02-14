## 概要
VRChatのアバター改変向けに、衣装などの複数のアセットの中から、改変時にリソースとして様々なものに流用できるテクスチャに容易にアクセスできるようにする拡張機能を開発したい。

## 要件
- ツールタブ内で、種類ごとのテクスチャリソースに横断的かつ高い検索性でアクセスできるようにする
- 種類はまず基本としてMatcapと、タイリング可能なテクスチャ（マスクなど）とタイリング可能なノーマルマップを扱う
- 上記のテクスチャの種類の判別は、インポートしたアセット内のマテリアルでの使われ方を元に判別する（lilToon想定）
- Matcapスロットで使用されている->Matcap
- メインテクスチャスロットやエミッションなど、タイリング可能な項目でタイリングが有効になっている->タイリング可能なテクスチャ
- ノーマルスロットで使用されていてタイリングが2×2以上になっている->タイリング可能なノーマルマップ
- リソースはguidで参照先を管理する（リソースの複製などはプロジェクトサイズの増大につながるため避けるが、オプションで一か所にリソースを複製して整理できるようにする）

## Texture Management Idea: SharedTexHub

## Core Concept
- **Purpose**: A centralized hub for accessing shared textures (MatCap, Tiling, Normal Maps, Masks, Decals) across the entire project.
- **Problem Solved**: Eliminates the need to hunt for specific textures buried deep in asset folders or re-import duplicates.
- **Target Audience**: VRChat avatar creators and users who frequently modify materials.
- **Key Feature**: Automatic detection of textures used in materials (specifically lilToon shader) AND manual registration via dedicated folders.

## Features

### 1. Automatic Detection
- Automatically scans the project for materials using supported shaders (e.g., lilToon).
- Extracts relevant textures based on property names (`_MatCapTex`, `_MainTex`, `_BumpMap`, etc.).
- Categorizes found textures into tabs: MatCap, Tiling, Normal, Mask, Decal.
- **Deduplication**: Identifies unique textures by file hash, ensuring the same image appears only once even if duplicated in the project.

### 2. Manual Registration (New!)
- **Dedicated Folders**: Users can manually place textures into specific folders to include them in the library, even if not used in any material.
    - Path: `Assets/SharedTexHub/[CategoryName]/` (e.g., `Assets/SharedTexHub/MatCap/`)
- **Context Menu Integration**:
    - Right-click on a texture or folder in the Project view -> `SharedTexHub > Add to Library > [Category]`.
    - This action **copies** the selected asset(s) to the corresponding dedicated folder.
- **Direct Access**:
    - "Open Folder" button in the SharedTexHub UI to open the dedicated folder in the OS file explorer.

### 3. Smart Sorting & Organization
- **Color Analysis**: Analyzes texture colors (Average Color, Hue, Saturation, Brightness).
- **Sorting Options**:
    - **Name**: Alphabetical by file path.
    - **Color**: Sort by hue, then saturation/brightness. Supports separation of grayscale images.
    - **Color Spread**: Sort by "busyness" or color variance (single color vs. rainbow/gradient), then by hue.
- **Quantization**: Uses quantized values (e.g., 15-degree hue steps) for grouping similar colors.

### 4. Usability Enhancements
- **Drag & Drop**: Drag textures directly from the hub to Material Inspectors or the Scene view.
- **Copy to Local**: Ability to copy a selected texture to a local folder for modification.
- **UI Scaling**: Slider to adjust thumbnail size (50px - 200px).
- **Persistent Data**: Texture metadata (hash, color info) is cached for performance.

## Technical Implementation (Update)

- **Directory Structure**:
    - `Assets/Editor/SharedTexHub/`: Main tool logic and UI.
    - `Assets/SharedTexHub/`: **(New)** User-facing folder for manual texture registration. Not ignored by git (unless configured by user).
    - `Assets/Editor/SharedTexHub/Data/`: Internal database and cache (Ignored by git).

- **Scanner Logic**:
    - **Material Scanner**: Existing logic to scan materials.
    - **Folder Scanner**: **(New)** logic to scan `Assets/SharedTexHub/[Category]/` recursively.

- **Data Management**:
    - `SharedTexHubData.asset`: Stores `TextureInfo` list.
    - `HashGenerator`: Handles MD5 hashing for deduplication.

## Future Possibilities
- Support for more shaders (Poiyomi, standard, etc.).
- Tagging system for user-defined categories.
- Cloud sync? (Maybe out of scope).
