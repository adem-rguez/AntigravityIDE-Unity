# Changelog

All notable changes to the `com.antigravity.ide` package will be documented in this file.

## [1.0.0] - 2026-09-13

### Added
- Initial release of the Antigravity IDE Editor Integration package for Unity.
- Implementation of Unity's `Unity.CodeEditor.IExternalCodeEditor` API.
- Full workspace loading with script and line jumping (`--goto` / `-r`).
- Automatic `.sln` and `.csproj` solution generation for Unity assemblies, Assembly Definitions (`.asmdef`), Packages, and Roslyn analyzers.
- OS discovery for Antigravity IDE installations on Windows, macOS, and Linux.
- Workspace configuration generator for `.vscode/settings.json` and `.vscode/launch.json` (Unity Attach Debugger).
- Preferences UI in Unity Preferences > External Tools for granular package project generation settings.
