using System;

namespace FilmCRUD.CustomExceptions
{
    public class FileNameParserError : Exception
    {
        public FileNameParserError()
        {
        }

        public FileNameParserError(string message)
            : base(message)
        {
        }

        public FileNameParserError(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}