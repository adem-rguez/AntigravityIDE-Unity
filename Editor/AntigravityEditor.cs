using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Antigravity.Editor.ProjectGeneration;
using UnityEditor;
using UnityEditor.CodeEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Antigravity.Editor
{
    [InitializeOnLoad]
    public class AntigravityEditor : IExternalCodeEditor
    {
        private const string k_EditorName = "Antigravity IDE";
        private const string k_DefaultArgument = "\"{project}\" -r -g \"{file}:{line}:{column}\"";
        private const string k_DefaultProjectArgument = "\"{project}\"";

        private static readonly string[] k_SupportedExtensions = new[]
        {
            "cs", "shader", "compute", "cginc", "hlsl", "glslinc", "template", "raytrace",
            "json", "asmdef", "asmref", "rsp", "xml", "txt", "md"
        };

        private readonly IGenerator m_ProjectGeneration;

        static AntigravityEditor()
        {
            CodeEditor.Register(new AntigravityEditor());
        }

        public AntigravityEditor()
        {
            m_ProjectGeneration = new ProjectGeneration.ProjectGeneration(Directory.GetParent(Application.dataPath).FullName);
        }

        public AntigravityEditor(IGenerator projectGeneration)
        {
            m_ProjectGeneration = projectGeneration;
        }

        public CodeEditor.Installation[] Installations
        {
            get
            {
                var discovered = AntigravityDiscovery.FindInstallations();
                return discovered.Select(inst => new CodeEditor.Installation
                {
                    Name = inst.Name,
                    Path = inst.Path
                }).ToArray();
            }
        }

        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            if (AntigravityDiscovery.IsAntigravityPath(editorPath))
            {
                installation = new CodeEditor.Installation
                {
                    Name = k_EditorName,
                    Path = editorPath
                };
                return true;
            }

            var matching = Installations.FirstOrDefault(i => string.Equals(i.Path, editorPath, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(matching.Path))
            {
                installation = matching;
                return true;
            }

            installation = default;
            return false;
        }

        public void Initialize(string editorInstallationPath)
        {
            // Sync solution files and generate workspace config when initialized
            SyncAll();
            WorkspaceConfigGenerator.GenerateWorkspaceConfig(m_ProjectGeneration.ProjectDirectory);
        }

        public bool OpenProject(string filePath = "", int line = -1, int column = -1)
        {
            var editorPath = CodeEditor.CurrentEditorInstallation;
            if (string.IsNullOrEmpty(editorPath))
            {
                Debug.LogError("[Antigravity IDE] Editor installation path is not set.");
                return false;
            }

            var projectDir = m_ProjectGeneration.ProjectDirectory;

            // Make sure project files and workspace config are synced
            if (!m_ProjectGeneration.HasSolutionBeenGenerated())
            {
                SyncAll();
            }
            WorkspaceConfigGenerator.GenerateWorkspaceConfig(projectDir);

            var arguments = BuildArguments(projectDir, filePath, line, column);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = editorPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = projectDir
                };

                // On macOS if path is a .app bundle, use 'open' command
                if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOS && editorPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.FileName = "open";
                    startInfo.Arguments = $"-n -b com.google.antigravity --args {arguments}";
                }

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity IDE] Failed to open Antigravity IDE at path '{editorPath}': {ex.Message}");
                return false;
            }
        }

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            m_ProjectGeneration.SyncIfNeeded(addedFiles, deletedFiles, movedFiles, movedFromFiles, importedFiles);
        }

        public void SyncAll()
        {
            m_ProjectGeneration.Sync();
        }

        public void OnGUI()
        {
            GUILayout.Label("Antigravity IDE Project Generation Settings", EditorStyles.boldLabel);

            var provider = m_ProjectGeneration.AssemblyNameProvider;

            EditorGUI.BeginChangeCheck();

            var toggleEmbedded = EditorGUILayout.Toggle("Embedded Packages", provider.IsProjectGenerationFlagEnabled(ProjectGenerationFlag.Embedded));
            var toggleLocal = EditorGUILayout.Toggle("Local Packages", provider.IsProjectGenerationFlagEnabled(ProjectGenerationFlag.Local));
            var toggleRegistry = EditorGUILayout.Toggle("Registry Packages", provider.IsProjectGenerationFlagEnabled(ProjectGenerationFlag.Registry));
            var toggleGit = EditorGUILayout.Toggle("Git Packages", provider.IsProjectGenerationFlagEnabled(ProjectGenerationFlag.Git));
            var toggleBuiltIn = EditorGUILayout.Toggle("Built-in Packages", provider.IsProjectGenerationFlagEnabled(ProjectGenerationFlag.BuiltIn));
            var togglePlayerAssemblies = EditorGUILayout.Toggle("Player Projects", provider.IsProjectGenerationFlagEnabled(ProjectGenerationFlag.PlayerAssemblies));

            if (EditorGUI.EndChangeCheck())
            {
                SetFlag(provider, ProjectGenerationFlag.Embedded, toggleEmbedded);
                SetFlag(provider, ProjectGenerationFlag.Local, toggleLocal);
                SetFlag(provider, ProjectGenerationFlag.Registry, toggleRegistry);
                SetFlag(provider, ProjectGenerationFlag.Git, toggleGit);
                SetFlag(provider, ProjectGenerationFlag.BuiltIn, toggleBuiltIn);
                SetFlag(provider, ProjectGenerationFlag.PlayerAssemblies, togglePlayerAssemblies);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Regenerate Project Files (.sln / .csproj)", GUILayout.Width(280)))
            {
                SyncAll();
                WorkspaceConfigGenerator.GenerateWorkspaceConfig(m_ProjectGeneration.ProjectDirectory);
                Debug.Log("[Antigravity IDE] Successfully regenerated .sln and .csproj solution files.");
            }
        }

        private void SetFlag(IAssemblyNameProvider provider, ProjectGenerationFlag flag, bool value)
        {
            if (provider.IsProjectGenerationFlagEnabled(flag) != value)
            {
                provider.ToggleProjectGeneration(flag);
            }
        }

        private string BuildArguments(string projectDir, string filePath, int line, int column)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return $"\"{projectDir}\"";
            }

            var fullPath = Path.IsPathRooted(filePath) ? filePath : Path.GetFullPath(Path.Combine(projectDir, filePath));

            var lineArg = line > 0 ? line : 1;
            var colArg = column > 0 ? column : 1;

            return $"\"{projectDir}\" -r -g \"{fullPath}:{lineArg}:{colArg}\"";
        }
    }
}
