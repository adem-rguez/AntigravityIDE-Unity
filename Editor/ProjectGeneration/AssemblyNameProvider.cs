using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;

namespace Antigravity.Editor.ProjectGeneration
{
    public class AssemblyNameProvider : IAssemblyNameProvider
    {
        private ProjectGenerationFlag m_ProjectGenerationFlag = (ProjectGenerationFlag)0x7FFFFFFF;

        public string[] ProjectSupportedExtensions => EditorSettings.projectGenerationBuiltinextensions;

        public string ProjectGenerationRootNamespace => EditorSettings.projectGenerationRootNamespace;

        public string GetProjectName()
        {
            return Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath("Assets")));
        }

        public IEnumerable<Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfAssembly)
        {
            var assemblies = new List<Assembly>();
            assemblies.AddRange(CompilationPipeline.GetAssemblies(AssembliesType.Editor));
            
            if (IsProjectGenerationFlagEnabled(ProjectGenerationFlag.PlayerAssemblies))
            {
                assemblies.AddRange(CompilationPipeline.GetAssemblies(AssembliesType.Player));
            }

            return assemblies.Where(a => a.sourceFiles.Any(shouldFileBePartOfAssembly));
        }

        public IEnumerable<Assembly> GetEditorAssemblies()
        {
            return CompilationPipeline.GetAssemblies(AssembliesType.Editor);
        }

        public string GetAssemblyNameFromScriptPath(string path)
        {
            return CompilationPipeline.GetAssemblyNameFromScriptPath(path);
        }

        public IEnumerable<UnityEditor.PackageManager.PackageInfo> GetAllPackages()
        {
            return UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
        }

        public UnityEditor.PackageManager.PackageInfo FindForAssetPath(string assetPath)
        {
            return UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
        }

        public ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories)
        {
            return CompilationPipeline.ParseResponseFile(responseFilePath, projectDirectory, systemReferenceDirectories);
        }

        public void ToggleProjectGeneration(ProjectGenerationFlag flag)
        {
            m_ProjectGenerationFlag ^= flag;
        }

        public bool IsProjectGenerationFlagEnabled(ProjectGenerationFlag flag)
        {
            return (m_ProjectGenerationFlag & flag) == flag;
        }
    }
}
