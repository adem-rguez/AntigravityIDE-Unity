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

            GenerateSettingsJson(vscodeDir, projectDirectory);
            GenerateLaunchJson(vscodeDir);
            GenerateExtensionsJson(vscodeDir);
        }

        private static void GenerateSettingsJson(string vscodeDir, string projectDirectory)
        {
            var settingsPath = Path.Combine(vscodeDir, "settings.json");
            var projectName = Path.GetFileName(projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var solutionName = $"{projectName}.sln";

            var settingsContent = $@"{{
    ""omnisharp.solutionPath"": ""{solutionName}"",
    ""omnisharp.defaultSolution"": ""{solutionName}"",
    ""dotnet.defaultSolution"": ""{solutionName}"",
    ""omnisharp.useModernNet"": false,
    ""omnisharp.enableRoslynAnalyzers"": true,
    ""omnisharp.enableEditorConfigSupport"": true,
    ""omnisharp.autoStart"": true,
    ""csharp.referencesCodeLens.enabled"": true,
    ""csharp.implementationsCodeLens.enabled"": true,
    ""csharp.showReferencesAtDeclaration"": true,
    ""csharp.inlayHints.csharpAll"": true,
    ""editor.codeLens"": true,
    ""csharp.semanticHighlighting.enabled"": true,
    ""files.exclude"": {{
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
    }},
    ""explorer.fileNesting.enabled"": true,
    ""explorer.fileNesting.expand"": false,
    ""explorer.fileNesting.patterns"": {{
        ""*.cs"": ""$(capture).cs.meta"",
        ""*.prefab"": ""$(capture).prefab.meta"",
        ""*.mat"": ""$(capture).mat.meta"",
        ""*.asset"": ""$(capture).asset.meta"",
        ""*.unity"": ""$(capture).unity.meta""
    }}
}}";
            File.WriteAllText(settingsPath, settingsContent, Encoding.UTF8);
        }

        private static void GenerateLaunchJson(string vscodeDir)
        {
            var launchPath = Path.Combine(vscodeDir, "launch.json");
            if (File.Exists(launchPath))
                return;

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

        private static void GenerateExtensionsJson(string vscodeDir)
        {
            var extensionsPath = Path.Combine(vscodeDir, "extensions.json");
            if (File.Exists(extensionsPath))
                return;

            var extensionsContent = @"{
    ""recommendations"": [
        ""muhammad-sammy.csharp"",
        ""ms-dotnettools.csharp"",
        ""visualstudiotoolsforunity.vstuc""
    ]
}";
            File.WriteAllText(extensionsPath, extensionsContent, Encoding.UTF8);
        }
    }
}
