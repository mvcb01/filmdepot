using System;

namespace FilmCRUD.CustomExceptions
{
    public class MultipleSearchResultsError : Exception
    {
        public MultipleSearchResultsError()
        {
        }

        public MultipleSearchResultsError(string message)
            : base(message)
        {
        }

        public MultipleSearchResultsError(string message, MultipleSearchResultsError inner)
            : base(message, inner)
        {
        }
    }
}