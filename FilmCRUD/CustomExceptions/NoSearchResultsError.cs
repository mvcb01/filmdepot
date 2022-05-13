using System;

namespace FilmCRUD.CustomExceptions
{
    public class NoSearchResultsError : Exception
    {
        public NoSearchResultsError()
        {
        }

        public NoSearchResultsError(string message)
            : base(message)
        {
        }

        public NoSearchResultsError(string message, NoSearchResultsError inner)
            : base(message, inner)
        {
        }
    }
}