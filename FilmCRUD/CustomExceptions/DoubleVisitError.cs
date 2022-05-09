using System;

namespace FilmCRUD.CustomExceptions
{
    public class DoubleVisitError : Exception
    {
        public DoubleVisitError()
        {
        }

        public DoubleVisitError(string message)
            : base(message)
        {
        }

        public DoubleVisitError(string message, DoubleVisitError inner)
            : base(message, inner)
        {
        }
    }
}