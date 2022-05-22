using System;

namespace FilmCRUD.CustomExceptions
{
    public class MultipleMovieMatchesError : Exception
    {
        public MultipleMovieMatchesError()
        {
        }

        public MultipleMovieMatchesError(string message)
            : base(message)
        {
        }

        public MultipleMovieMatchesError(string message, MultipleMovieMatchesError inner)
            : base(message, inner)
        {
        }
    }

}