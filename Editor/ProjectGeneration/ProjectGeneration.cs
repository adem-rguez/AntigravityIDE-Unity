using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;

namespace Antigravity.Editor.ProjectGeneration
{
    public class ProjectGeneration : IGenerator
    {
        public static readonly string[] DefaultExtensions = new[] { "cs", "shader", "compute", "cginc", "hlsl", "glslinc", "template", "raytrace" };

        private readonly string m_ProjectDirectory;
        private readonly IAssemblyNameProvider m_AssemblyNameProvider;
        private readonly IFileIO m_FileIO;

        private const string k_SolutionProjectEntryTemplate = @"Project(""{0}"") = ""{1}"", ""{2}"", ""{3}""{4}EndProject";
        private const string k_SolutionHeader = @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 15
VisualStudioVersion = 15.0.26124.0
MinimumVisualStudioVersion = 15.0.26124.0";

        private const string k_CsprojTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <LangVersion>{0}</LangVersion>
    <DefaultLanguage>en-US</DefaultLanguage>
    <_TargetFrameworkDirectories>non_empty_path_generated_by_unity_editor</_TargetFrameworkDirectories>
    <_FullFrameworkReferenceAssemblyPaths>non_empty_path_generated_by_unity_editor</_FullFrameworkReferenceAssemblyPaths>
  </PropertyGroup>
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>10.0.20506</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <RootNamespace>{1}</RootNamespace>
    <ProjectGuid>{{{2}}}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <AssemblyName>{3}</AssemblyName>
    <TargetFrameworkVersion>{4}</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <BaseDirectory>.</BaseDirectory>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>Temp\Bin\Debug\{3}\</OutputPath>
    <DefineConstants>{5}</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <NoWarn>0169,0219,0414,0649,0618</NoWarn>
    <AllowUnsafeBlocks>{6}</AllowUnsafeBlocks>
    <Nullable>{7}</Nullable>
  </PropertyGroup>
  <ItemGroup>
{8}
  </ItemGroup>
  <ItemGroup>
{9}
  </ItemGroup>
  <ItemGroup>
{10}
  </ItemGroup>
  <ItemGroup>
{11}
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";

        public string ProjectDirectory => m_ProjectDirectory;
        public IAssemblyNameProvider AssemblyNameProvider => m_AssemblyNameProvider;

        public ProjectGeneration(string projectDirectory)
            : this(projectDirectory, new AssemblyNameProvider(), new FileIO())
        {
        }

        public ProjectGeneration(string projectDirectory, IAssemblyNameProvider assemblyNameProvider, IFileIO fileIO)
        {
            m_ProjectDirectory = projectDirectory;
            m_AssemblyNameProvider = assemblyNameProvider;
            m_FileIO = fileIO;
        }

        public bool HasSolutionBeenGenerated()
        {
            return m_FileIO.Exists(SolutionFile());
        }

        public bool IsSupportedFile(string path)
        {
            var extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
                return false;

            extension = extension.TrimStart('.');
            if (DefaultExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return true;

            var supported = m_AssemblyNameProvider.ProjectSupportedExtensions;
            return supported != null && supported.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        public void GenerateAll(bool generateAll)
        {
            Sync();
        }

        public string SolutionFile()
        {
            return Path.Combine(m_ProjectDirectory, $"{m_AssemblyNameProvider.GetProjectName()}.sln");
        }

        public void Sync()
        {
            var assemblies = m_AssemblyNameProvider.GetAssemblies(IsSupportedFile).ToList();
            var allPackages = m_AssemblyNameProvider.GetAllPackages().ToList();

            SyncSolution(assemblies);

            foreach (var assembly in assemblies)
            {
                SyncProject(assembly, assemblies, allPackages);
            }
        }

        public bool SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            if (!HasSolutionBeenGenerated())
            {
                Sync();
                return true;
            }

            var allAffected = (addedFiles ?? Array.Empty<string>())
                .Concat(deletedFiles ?? Array.Empty<string>())
                .Concat(movedFiles ?? Array.Empty<string>())
                .Concat(movedFromFiles ?? Array.Empty<string>())
                .Concat(importedFiles ?? Array.Empty<string>());

            if (allAffected.Any(IsSupportedFile) || allAffected.Any(f => f.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase)))
            {
                Sync();
                return true;
            }

            return false;
        }

        private void SyncSolution(IEnumerable<Assembly> assemblies)
        {
            var solutionBuilder = new StringBuilder();
            solutionBuilder.AppendLine(k_SolutionHeader);

            var projectGuidType = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"; // C# Project Type GUID
            var assemblyList = assemblies.ToList();

            foreach (var assembly in assemblyList)
            {
                var projectPath = $"{assembly.name}.csproj";
                var projectGuid = GetProjectGuid(assembly.name);
                solutionBuilder.AppendLine(string.Format(
                    k_SolutionProjectEntryTemplate,
                    projectGuidType,
                    assembly.name,
                    projectPath,
                    $"{{{projectGuid}}}",
                    Environment.NewLine
                ));
            }

            solutionBuilder.AppendLine("Global");
            solutionBuilder.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            solutionBuilder.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
            solutionBuilder.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
            solutionBuilder.AppendLine("\tEndGlobalSection");

            solutionBuilder.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (var assembly in assemblyList)
            {
                var projectGuid = $"{{{GetProjectGuid(assembly.name)}}}";
                solutionBuilder.AppendLine($"\t\t{projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
                solutionBuilder.AppendLine($"\t\t{projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU");
                solutionBuilder.AppendLine($"\t\t{projectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU");
                solutionBuilder.AppendLine($"\t\t{projectGuid}.Release|Any CPU.Build.0 = Release|Any CPU");
            }
            solutionBuilder.AppendLine("\tEndGlobalSection");

            solutionBuilder.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
            solutionBuilder.AppendLine("\t\thideSolutionNode = FALSE");
            solutionBuilder.AppendLine("\tEndGlobalSection");
            solutionBuilder.AppendLine("EndGlobal");

            m_FileIO.WriteAllText(SolutionFile(), solutionBuilder.ToString());
        }

        private void SyncProject(Assembly assembly, List<Assembly> allAssemblies, List<UnityEditor.PackageManager.PackageInfo> allPackages)
        {
            var projectFile = Path.Combine(m_ProjectDirectory, $"{assembly.name}.csproj");

            var langVersion = GetLangVersion(assembly);
            var rootNamespace = m_AssemblyNameProvider.ProjectGenerationRootNamespace ?? "";
            var projectGuid = GetProjectGuid(assembly.name);
            var targetFramework = GetTargetFramework();
            var defineConstants = string.Join(";", assembly.defines.Concat(new[] { "TRACE", "DEBUG" }).Distinct());
            var allowUnsafe = (assembly.compilerOptions.AllowUnsafeCode).ToString().ToLowerInvariant();
            var nullable = GetNullableSetting(assembly);

            var compilesBuilder = new StringBuilder();
            foreach (var sourceFile in assembly.sourceFiles)
            {
                if (IsSupportedFile(sourceFile))
                {
                    var normalized = NormalizePath(sourceFile);
                    compilesBuilder.AppendLine($"    <Compile Include=\"{EscapeXml(normalized)}\" />");
                }
            }

            var projectReferencesBuilder = new StringBuilder();
            var assemblyReferencesBuilder = new StringBuilder();

            var projectAssemblyNames = new HashSet<string>(allAssemblies.Select(a => a.name));

            foreach (var assemblyRef in assembly.assemblyReferences)
            {
                if (projectAssemblyNames.Contains(assemblyRef.name))
                {
                    var refGuid = GetProjectGuid(assemblyRef.name);
                    projectReferencesBuilder.AppendLine($@"    <ProjectReference Include=""{assemblyRef.name}.csproj"">
      <Project>{{{refGuid}}}</Project>
      <Name>{assemblyRef.name}</Name>
    </ProjectReference>");
                }
                else if (!string.IsNullOrEmpty(assemblyRef.outputPath))
                {
                    var normalizedPath = NormalizePath(assemblyRef.outputPath);
                    assemblyReferencesBuilder.AppendLine($@"    <Reference Include=""{assemblyRef.name}"">
      <HintPath>{EscapeXml(normalizedPath)}</HintPath>
    </Reference>");
                }
            }

            foreach (var compiledRef in assembly.compiledAssemblyReferences)
            {
                var refName = Path.GetFileNameWithoutExtension(compiledRef);
                var normalizedPath = NormalizePath(compiledRef);
                assemblyReferencesBuilder.AppendLine($@"    <Reference Include=""{refName}"">
      <HintPath>{EscapeXml(normalizedPath)}</HintPath>
    </Reference>");
            }

            var analyzersBuilder = new StringBuilder();
            if (assembly.compilerOptions.RoslynAnalyzerDllPaths != null)
            {
                foreach (var analyzer in assembly.compilerOptions.RoslynAnalyzerDllPaths)
                {
                    var normalizedPath = NormalizePath(analyzer);
                    analyzersBuilder.AppendLine($"    <Analyzer Include=\"{EscapeXml(normalizedPath)}\" />");
                }
            }

            var content = string.Format(
                k_CsprojTemplate,
                langVersion,
                rootNamespace,
                projectGuid,
                assembly.name,
                targetFramework,
                defineConstants,
                allowUnsafe,
                nullable,
                compilesBuilder.ToString().TrimEnd(),
                projectReferencesBuilder.ToString().TrimEnd(),
                assemblyReferencesBuilder.ToString().TrimEnd(),
                analyzersBuilder.ToString().TrimEnd()
            );

            m_FileIO.WriteAllText(projectFile, content);
        }

        private string GetLangVersion(Assembly assembly)
        {
            var match = Regex.Match(assembly.compilerOptions.LanguageVersion ?? "", @"\d+(\.\d+)?");
            if (match.Success)
            {
                return match.Value;
            }

#if UNITY_2021_2_OR_NEWER
            return "9.0";
#elif UNITY_2020_2_OR_NEWER
            return "8.0";
#else
            return "7.3";
#endif
        }

        private string GetTargetFramework()
        {
#if UNITY_2021_2_OR_NEWER
            return "netstandard2.1";
#else
            return "v4.7.1";
#endif
        }

        private string GetNullableSetting(Assembly assembly)
        {
#if UNITY_2020_2_OR_NEWER
            if (assembly.compilerOptions.Nullable != null)
            {
                return assembly.compilerOptions.Nullable.ToString().ToLowerInvariant();
            }
#endif
            return "disable";
        }

        private string GetProjectGuid(string assemblyName)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(assemblyName + "_" + m_ProjectDirectory);
                var hashBytes = md5.ComputeHash(inputBytes);
                return new Guid(hashBytes).ToString().ToUpperInvariant();
            }
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            var fullPath = Path.GetFullPath(path);
            var projectFullPath = Path.GetFullPath(m_ProjectDirectory);

            if (fullPath.StartsWith(projectFullPath, StringComparison.OrdinalIgnoreCase))
            {
                var rel = fullPath.Substring(projectFullPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return rel.Replace('/', '\\');
            }

            return fullPath.Replace('/', '\\');
        }

        private string EscapeXml(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
