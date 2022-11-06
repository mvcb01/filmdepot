using System.Collections.Generic;
using System.IO;
using FilmCRUD.Interfaces;

namespace FilmCRUD.Helpers
{
    public class FileSystemIOWrapper : IFileSystemIOWrapper
    {
        public void CreateDirectory(string dirPath)
        {
            Directory.CreateDirectory(dirPath);
        }

        public bool DirectoryExists(string dirPath)
        {
            return Directory.Exists(dirPath);
        }

        public bool FileExist(string fPath)
        {
            return File.Exists(fPath);
        }

        public IEnumerable<string> GetFiles(string dirPath)
        {
            return Directory.GetFiles(dirPath);
        }

        public IEnumerable<string> GetSubdirectories(string dirPath)
        {
            return Directory.GetDirectories(dirPath);
        }

        public IEnumerable<string> ReadAllLines(string fPath)
        {
            return File.ReadAllLines(fPath);
        }

        public string ReadAllText(string fPath)
        {
            return File.ReadAllText(fPath);
        }

        public void WriteAllText(string fPath, string text)
        {
            File.WriteAllText(fPath, text);
        }
    }
}