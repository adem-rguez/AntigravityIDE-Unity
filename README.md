# Antigravity IDE Integration for Unity

[![Unity 2020.3+](https://img.shields.io/badge/Unity-2020.3%2B-blue.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A modern, high-performance Unity Editor package providing seamless integration with **Antigravity IDE**.

---

## Key Features

- **Full Project Workspace Loading**: Opening any script opens the entire Unity project workspace instead of isolated single files.
- **Accurate Code Navigation & IntelliSense**: Generates clean `.sln` and `.csproj` solution files targeting Unity assemblies, Assembly Definitions (`.asmdef`), Packages, Roslyn analyzers, and define symbols. Enables **Find All References**, **Go to Definition**, **Rename Symbol**, and type info out-of-the-box.
- **Direct Script Jumping (`--goto`)**: Double-clicking compiler errors or scripts navigates straight to the exact line and column inside your active Antigravity IDE window without spawning duplicate instances.
- **Automatic IDE Discovery**: Automatically detects Antigravity IDE on Windows, macOS, and Linux.
- **Automatic Workspace Setup**: Creates and manages `.vscode/settings.json` and `.vscode/launch.json` for Unity Debugger attachment and C# language server optimization.
- **Package Preferences UI**: Configure project generation for Embedded, Local, Git, Registry, and Built-in packages under Unity Preferences.

---

## Installation

### Method 1: Unity Package Manager (Git URL)
1. In Unity, open **Window > Package Manager**.
2. Click the **`+`** icon in the top-left corner.
3. Select **Add package from git URL...**.
4. Paste the Git repository URL:
   ```text
   https://github.com/adem2/AntigravityIDE-Unity.git
   ```
5. Click **Add**.

### Method 2: Manual / Local Disk
1. Download or clone this repository to a folder on your computer (or directly into your project's `Packages/` folder).
2. In Unity, open **Window > Package Manager**.
3. Click the **`+`** icon and choose **Add package from disk...**.
4. Select the `package.json` file in this repository.

### Method 3: `manifest.json`
Add the package reference to your project's `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.antigravity.ide": "https://github.com/adem2/AntigravityIDE-Unity.git",
    "...": "..."
  }
}
```

---

## Setup & Configuration

1. In Unity, navigate to **Edit > Preferences** (or **Unity > Settings** on macOS).
2. Select **External Tools** from the sidebar.
3. In the **External Script Editor** dropdown, select **Antigravity IDE**.
   - *If it is not automatically listed, click **Browse...** and select your `antigravity.exe` / `Antigravity.app`.*
4. Click **Regenerate project files**.

---

## Enabling C# Language Server & IntelliSense in Antigravity IDE

To get full C# code completion, references, and diagnostics in Antigravity IDE:

1. In Antigravity IDE, open the **Extensions** view (`Ctrl+Shift+X` / `Cmd+Shift+X`).
2. Install the **C#** extension (`ms-dotnettools.csharp`) or **Unity** extension (`visualstudiotoolsforunity.vstuc`).
3. When prompted to load the solution, select the `.sln` file generated in your Unity project root.

---

## Debugging C# Scripts

1. In Antigravity IDE, switch to the **Run & Debug** panel (`Ctrl+Shift+D` / `Cmd+Shift+D`).
2. Select **Attach to Unity Editor** from the launch target dropdown.
3. Press **F5** to attach the debugger.
4. Set your breakpoints and play your scene in Unity!

---

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
