using System;
using System.Collections.Generic;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;

namespace Antigravity.Editor.ProjectGeneration
{
    public interface IAssemblyNameProvider
    {
        string[] ProjectSupportedExtensions { get; }
        string ProjectGenerationRootNamespace { get; }
        
        IEnumerable<Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfAssembly);
        IEnumerable<Assembly> GetEditorAssemblies();
        string GetAssemblyNameFromScriptPath(string path);
        IEnumerable<UnityEditor.PackageManager.PackageInfo> GetAllPackages();
        UnityEditor.PackageManager.PackageInfo FindForAssetPath(string assetPath);
        ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories);
        void ToggleProjectGeneration(ProjectGenerationFlag flag);
        bool IsProjectGenerationFlagEnabled(ProjectGenerationFlag flag);
        string GetProjectName();
    }

    [Flags]
    public enum ProjectGenerationFlag
    {
        None = 0,
        Embedded = 1,
        Local = 2,
        Registry = 4,
        Git = 8,
        BuiltIn = 16,
        Unknown = 32,
        PlayerAssemblies = 64,
        LocalTarBall = 128
    }
}
