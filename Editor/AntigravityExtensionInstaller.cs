using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Antigravity.Editor
{
    public static class AntigravityExtensionInstaller
    {
        private const string k_OpenVsxExtensionId = "muhammad-sammy.csharp";
        private const string k_GitHubVsixUrl = "https://github.com/OmniSharp/omnisharp-vscode/releases/download/v1.26.0/csharp-1.26.0-win32-x64.vsix";

        public static bool IsInstalling { get; private set; }

        public static bool IsExtensionInstalled()
        {
            var editorPath = Unity.CodeEditor.CodeEditor.CurrentEditorInstallation;
            if (string.IsNullOrEmpty(editorPath) || !File.Exists(editorPath))
                return false;

            // Check extensions directories
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var extensionDirs = new[]
            {
                Path.Combine(userHome, ".antigravity", "extensions"),
                Path.Combine(userHome, ".gemini", "antigravity-ide", "extensions"),
                Path.Combine(userHome, ".vscode", "extensions")
            };

            foreach (var dir in extensionDirs)
            {
                if (!Directory.Exists(dir))
                    continue;

                var subDirs = Directory.GetDirectories(dir);
                foreach (var sub in subDirs)
                {
                    var name = Path.GetFileName(sub).ToLowerInvariant();
                    if (name.Contains("csharp") || name.Contains("omnisharp") || name.Contains("muhammad-sammy"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void InstallExtensionAsync(Action onComplete = null)
        {
            if (IsInstalling)
            {
                Debug.LogWarning("[Antigravity IDE] Extension installation is already in progress.");
                return;
            }

            var editorPath = Unity.CodeEditor.CodeEditor.CurrentEditorInstallation;
            if (string.IsNullOrEmpty(editorPath))
            {
                Debug.LogError("[Antigravity IDE] Cannot install extension: Antigravity IDE path is not configured.");
                return;
            }

            IsInstalling = true;
            EditorUtility.DisplayProgressBar("Antigravity IDE", "Installing C# Language Extension...", 0.3f);

            Task.Run(() =>
            {
                bool success = false;
                try
                {
                    // Attempt 1: Install from marketplace CLI
                    Debug.Log($"[Antigravity IDE] Installing {k_OpenVsxExtensionId} via CLI...");
                    success = RunInstallCommand(editorPath, $"--install-extension {k_OpenVsxExtensionId} --force");

                    // Attempt 2: If marketplace fails, download verified VSIX from GitHub
                    if (!success)
                    {
                        Debug.Log("[Antigravity IDE] Marketplace install failed or offline. Downloading C# VSIX package directly...");
                        var tempVsixPath = Path.Combine(Path.GetTempPath(), "csharp-extension.vsix");

                        using (var client = new WebClient())
                        {
                            client.DownloadFile(k_GitHubVsixUrl, tempVsixPath);
                        }

                        if (File.Exists(tempVsixPath))
                        {
                            Debug.Log("[Antigravity IDE] Installing downloaded VSIX package...");
                            success = RunInstallCommand(editorPath, $"--install-extension \"{tempVsixPath}\" --force");
                            try { File.Delete(tempVsixPath); } catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity IDE] Error during C# extension installation: {ex.Message}");
                }
                finally
                {
                    IsInstalling = false;
                    EditorApplication.delayCall += () =>
                    {
                        EditorUtility.ClearProgressBar();
                        if (success)
                        {
                            Debug.Log("<color=green>[Antigravity IDE] C# Language Support was installed successfully! Restart Antigravity IDE if open.</color>");
                            EditorUtility.DisplayDialog("Antigravity IDE", "C# Language Extension was installed successfully!\n\nPlease restart Antigravity IDE if it is currently open.", "OK");
                        }
                        else
                        {
                            Debug.LogWarning("[Antigravity IDE] Automatic extension installation could not finish. Please install 'muhammad-sammy.csharp' manually via Extensions tab.");
                        }
                        onComplete?.Invoke();
                    };
                }
            });
        }

        private static bool RunInstallCommand(string editorPath, string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                // Prevent Electron from opening GUI window
                processInfo.EnvironmentVariables["ELECTRON_RUN_AS_NODE"] = "1";
                processInfo.EnvironmentVariables["ELECTRON_NO_ATTACH_CONSOLE"] = "1";

                var cliJsPath = FindCliJsPath(editorPath);
                var cmdPath = FindCmdScriptPath(editorPath);

                if (!string.IsNullOrEmpty(cliJsPath))
                {
                    // Direct Node invocation via Electron binary - 100% headless, guaranteed no window
                    processInfo.FileName = editorPath;
                    processInfo.Arguments = $"\"{cliJsPath}\" {arguments}";
                }
                else if (!string.IsNullOrEmpty(cmdPath) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    processInfo.FileName = "cmd.exe";
                    processInfo.Arguments = $"/c \"\"{cmdPath}\" {arguments}\"";
                }
                else
                {
                    processInfo.FileName = editorPath;
                    processInfo.Arguments = arguments;
                }

                using (var process = Process.Start(processInfo))
                {
                    if (process == null) return false;
                    process.WaitForExit(45000); // 45 seconds timeout
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Antigravity IDE] Command execution failed: {ex.Message}");
                return false;
            }
        }

        private static string FindCliJsPath(string editorPath)
        {
            if (string.IsNullOrEmpty(editorPath))
                return null;

            var dir = Directory.Exists(editorPath) ? editorPath : Path.GetDirectoryName(editorPath);
            if (string.IsNullOrEmpty(dir))
                return null;

            var candidates = new[]
            {
                Path.Combine(dir, "resources", "app", "out", "cli.js"),
                Path.Combine(dir, "Contents", "Resources", "app", "out", "cli.js"),
                Path.Combine(dir, "..", "resources", "app", "out", "cli.js"),
                Path.Combine(dir, "app", "out", "cli.js")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }

            return null;
        }

        private static string FindCmdScriptPath(string editorPath)
        {
            if (string.IsNullOrEmpty(editorPath))
                return null;

            var dir = Directory.Exists(editorPath) ? editorPath : Path.GetDirectoryName(editorPath);
            if (string.IsNullOrEmpty(dir))
                return null;

            var candidates = new[]
            {
                Path.Combine(dir, "bin", "antigravity.cmd"),
                Path.Combine(dir, "bin", "code.cmd"),
                Path.Combine(dir, "resources", "app", "bin", "antigravity.cmd"),
                Path.Combine(dir, "resources", "app", "bin", "code.cmd"),
                Path.Combine(dir, "..", "bin", "antigravity.cmd")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }

            return null;
        }
    }
}
