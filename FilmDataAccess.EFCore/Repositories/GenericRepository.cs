using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

using FilmDomain.Interfaces;
using FilmDomain.Enums;

namespace FilmDataAccess.EFCore.Repositories
{
    // nota em
    //      https://codewithmukesh.com/blog/repository-pattern-in-aspnet-core/
    // Also note that, for the ADD and Remove Functions, we just do the operation on the dbContext object.
    // But we are not yet commiting/updating/saving the changes to the database whatsover.
    // This is not something to be done in a Repository Class.
    // We would need Unit of Work Pattern for these cases where you commit data to the database.
    public class GenericRepository<TEntity> : IEntityRepository<TEntity> where TEntity : class
    {
        protected readonly SQLiteAppContext _context;

        public GenericRepository(SQLiteAppContext context) => this._context = context;

        public void Add(TEntity entity) => this._context.Set<TEntity>().Add(entity);

        public void AddRange(IEnumerable<TEntity> entities) => this._context.Set<TEntity>().AddRange(entities);

        public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate) => this._context.Set<TEntity>().Where(predicate);

        public IEnumerable<TEntity> GetAll() => this._context.Set<TEntity>();

        public TEntity GetById(int id) => this._context.Set<TEntity>().Find(id);

        public FilmDomainEntityState GetEntityState(TEntity entity) => (FilmDomainEntityState)this._context.Entry(entity).State;

        public void Remove(TEntity entity) => this._context.Set<TEntity>().Remove(entity);

        public void RemoveRange(IEnumerable<TEntity> entities) => this._context.Set<TEntity>().RemoveRange(entities);
    }
}