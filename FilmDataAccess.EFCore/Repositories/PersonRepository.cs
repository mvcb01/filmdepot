using FilmDomain.Entities;
using FilmDomain.Interfaces;

namespace FilmDataAccess.EFCore.Repositories
{
    public class PersonRepository : GenericRepository<Person>, IPersonRepository
    {
        public PersonRepository(SQLiteAppContext context) : base(context)
        {
        }
    }
}
