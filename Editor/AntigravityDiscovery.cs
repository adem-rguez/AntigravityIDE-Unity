using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Antigravity.Editor
{
    public static class AntigravityDiscovery
    {
        public struct Installation
        {
            public string Name;
            public string Path;
        }

        public static List<Installation> FindInstallations()
        {
            var installations = new List<Installation>();
            var pathsChecked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    FindWindowsInstallations(installations, pathsChecked);
                    break;
                case OperatingSystemFamily.MacOSX:
                    FindMacInstallations(installations, pathsChecked);
                    break;
                case OperatingSystemFamily.Linux:
                    FindLinuxInstallations(installations, pathsChecked);
                    break;
            }

            return installations;
        }

        public static bool IsAntigravityPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var fileName = Path.GetFileName(path).ToLowerInvariant();
            return fileName.Contains("antigravity");
        }

        private static void FindWindowsInstallations(List<Installation> installations, HashSet<string> pathsChecked)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var candidatePaths = new[]
            {
                Path.Combine(localAppData, "Programs", "Antigravity", "Antigravity.exe"),
                Path.Combine(localAppData, "Programs", "antigravity", "antigravity.exe"),
                Path.Combine(localAppData, "antigravity", "Antigravity.exe"),
                Path.Combine(programFiles, "Antigravity", "Antigravity.exe"),
                Path.Combine(programFilesX86, "Antigravity", "Antigravity.exe"),
                Path.Combine(localAppData, "Programs", "Antigravity IDE", "Antigravity.exe"),
                Path.Combine(userProfile, ".gemini", "antigravity-ide", "bin", "antigravity.cmd"),
                Path.Combine(localAppData, "Programs", "Antigravity", "bin", "antigravity.cmd")
            };

            foreach (var candidate in candidatePaths)
            {
                AddIfValid(candidate, "Antigravity IDE", installations, pathsChecked);
            }

            // Check PATH environment variable
            FindInPath("antigravity.cmd", "Antigravity IDE (PATH)", installations, pathsChecked);
            FindInPath("antigravity.exe", "Antigravity IDE (PATH)", installations, pathsChecked);
        }

        private static void FindMacInstallations(List<Installation> installations, HashSet<string> pathsChecked)
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var candidates = new[]
            {
                "/Applications/Antigravity.app",
                Path.Combine(userHome, "Applications", "Antigravity.app"),
                "/Applications/Antigravity IDE.app",
                Path.Combine(userHome, "Applications", "Antigravity IDE.app")
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate) && pathsChecked.Add(candidate))
                {
                    installations.Add(new Installation
                    {
                        Name = "Antigravity IDE",
                        Path = candidate
                    });
                }
            }

            FindInPath("antigravity", "Antigravity IDE (PATH)", installations, pathsChecked);
        }

        private static void FindLinuxInstallations(List<Installation> installations, HashSet<string> pathsChecked)
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var candidates = new[]
            {
                "/usr/bin/antigravity",
                "/usr/local/bin/antigravity",
                "/snap/bin/antigravity",
                Path.Combine(userHome, ".local", "bin", "antigravity")
            };

            foreach (var candidate in candidates)
            {
                AddIfValid(candidate, "Antigravity IDE", installations, pathsChecked);
            }

            FindInPath("antigravity", "Antigravity IDE (PATH)", installations, pathsChecked);
        }

        private static void AddIfValid(string path, string displayName, List<Installation> installations, HashSet<string> pathsChecked)
        {
            if (File.Exists(path) && pathsChecked.Add(path))
            {
                installations.Add(new Installation
                {
                    Name = displayName,
                    Path = Path.GetFullPath(path)
                });
            }
        }

        private static void FindInPath(string executableName, string displayName, List<Installation> installations, HashSet<string> pathsChecked)
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv))
                return;

            var paths = pathEnv.Split(Path.PathSeparator);
            foreach (var p in paths)
            {
                if (string.IsNullOrWhiteSpace(p))
                    continue;

                try
                {
                    var full = Path.Combine(p.Trim(), executableName);
                    if (File.Exists(full) && pathsChecked.Add(full))
                    {
                        installations.Add(new Installation
                        {
                            Name = displayName,
                            Path = Path.GetFullPath(full)
                        });
                    }
                }
                catch
                {
                    // Ignore path lookup errors
                }
            }
        }
    }
}
