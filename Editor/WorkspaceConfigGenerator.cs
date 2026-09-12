using System.IO;
using System.Text;
using UnityEngine;

namespace Antigravity.Editor
{
    public static class WorkspaceConfigGenerator
    {
        public static void GenerateWorkspaceConfig(string projectDirectory)
        {
            var vscodeDir = Path.Combine(projectDirectory, ".vscode");
            if (!Directory.Exists(vscodeDir))
            {
                Directory.CreateDirectory(vscodeDir);
            }

            GenerateSettingsJson(vscodeDir);
            GenerateLaunchJson(vscodeDir);
        }

        private static void GenerateSettingsJson(string vscodeDir)
        {
            var settingsPath = Path.Combine(vscodeDir, "settings.json");
            if (File.Exists(settingsPath))
                return; // Do not overwrite user's custom settings if already created

            var settingsContent = @"{
    ""dotnet.defaultSolution"": ""auto"",
    ""omnisharp.useModernNet"": true,
    ""omnisharp.enableRoslynAnalyzers"": true,
    ""omnisharp.enableEditorConfigSupport"": true,
    ""csharp.semanticHighlighting.enabled"": true,
    ""files.exclude"": {
        ""**/.git"": true,
        ""**/.svn"": true,
        ""**/.hg"": true,
        ""**/CVS"": true,
        ""**/.DS_Store"": true,
        ""**/Thumbs.db"": true,
        ""**/*.meta"": true,
        ""**/*.unityproj"": true,
        ""**/*.mat"": false,
        ""**/*.prefab"": false
    },
    ""explorer.fileNesting.enabled"": true,
    ""explorer.fileNesting.expand"": false,
    ""explorer.fileNesting.patterns"": {
        ""*.cs"": ""$(capture).cs.meta"",
        ""*.prefab"": ""$(capture).prefab.meta"",
        ""*.mat"": ""$(capture).mat.meta"",
        ""*.asset"": ""$(capture).asset.meta"",
        ""*.unity"": ""$(capture).unity.meta""
    }
}";
            File.WriteAllText(settingsPath, settingsContent, Encoding.UTF8);
        }

        private static void GenerateLaunchJson(string vscodeDir)
        {
            var launchPath = Path.Combine(vscodeDir, "launch.json");
            if (File.Exists(launchPath))
                return; // Do not overwrite if already created

            var launchContent = @"{
    ""version"": ""0.2.0"",
    ""configurations"": [
        {
            ""name"": ""Attach to Unity Editor"",
            ""type"": ""vstuc"",
            ""request"": ""attach""
        },
        {
            ""name"": ""Unity Debugger (Legacy/Mono)"",
            ""type"": ""unity"",
            ""request"": ""attach""
        }
    ]
}";
            File.WriteAllText(launchPath, launchContent, Encoding.UTF8);
        }
    }
}
