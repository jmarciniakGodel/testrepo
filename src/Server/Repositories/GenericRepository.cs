using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

/// <summary>
/// Generic repository implementation providing common CRUD operations
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    /// <summary>
    /// Database context
    /// </summary>
    protected readonly AppDbContext _context;

    /// <summary>
    /// DbSet for the entity type
    /// </summary>
    protected readonly DbSet<T> _dbSet;

    /// <summary>
    /// Initializes a new instance of the GenericRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    /// <summary>
    /// Retrieves all entities from the database
    /// </summary>
    /// <returns>Collection of all entities</returns>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    /// <summary>
    /// Retrieves a single entity by its identifier
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <returns>The entity if found, null otherwise</returns>
    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// Creates a new entity in the database
    /// </summary>
    /// <param name="entity">The entity to create</param>
    /// <returns>The created entity with generated values</returns>
    public virtual async Task<T> CreateAsync(T entity)
    {
        _dbSet.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Updates an existing entity in the database
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes an entity from the database
    /// </summary>
    /// <param name="id">The identifier of the entity to delete</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
