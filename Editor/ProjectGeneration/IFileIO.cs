using System.IO;

namespace Antigravity.Editor.ProjectGeneration
{
    public interface IFileIO
    {
        bool Exists(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string content);
        void CreateDirectory(string path);
    }
}
