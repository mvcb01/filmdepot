using System.Collections.Generic;

namespace FilmCRUD.Interfaces
{
    public interface IFileSystemIOWrapper
    {
        bool DirectoryExists(string dirPath);

        bool FileExist(string fPath);

        IEnumerable<string> GetSubdirectories(string dirPath);

        IEnumerable<string> GetFiles(string dirPath);

        string ReadAllText(string fPath);

        IEnumerable<string> ReadAllLines(string fPath);

        void WriteAllText(string fPath, string text);
    }
}