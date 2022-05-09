using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Interfaces;

namespace FilmDataAccess.EFCore.Repositories
{
    public class ActorRepository : GenericRepository<Actor>, IActorRepository
    {
        public ActorRepository(SQLiteAppContext context) : base(context)
        {
        }

        public IEnumerable<Actor> GetActorsFromName(string name)
        {
            return Find(a => a.Name.Contains(name));
        }
    }
}