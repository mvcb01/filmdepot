using System;

namespace FilmCRUD.CustomExceptions
{
    public class FileExistsError : Exception
    {
        public FileExistsError()
        {
        }

        public FileExistsError(string message)
            : base(message)
        {
        }

        public FileExistsError(string message, FileExistsError inner)
            : base(message, inner)
        {
        }
    }
}