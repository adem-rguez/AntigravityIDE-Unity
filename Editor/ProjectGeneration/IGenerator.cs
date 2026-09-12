namespace Antigravity.Editor.ProjectGeneration
{
    public interface IGenerator
    {
        bool HasSolutionBeenGenerated();
        bool IsSupportedFile(string path);
        void Sync();
        bool SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles);
        void GenerateAll(bool generateAll);
        string SolutionFile();
        string ProjectDirectory { get; }
        IAssemblyNameProvider AssemblyNameProvider { get; }
    }
}
