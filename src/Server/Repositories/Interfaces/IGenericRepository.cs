namespace Server.Repositories.Interfaces;

/// <summary>
/// Generic repository interface providing common CRUD operations for entities
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IGenericRepository<T> where T : class
{
    /// <summary>
    /// Retrieves all entities from the database
    /// </summary>
    /// <returns>Collection of all entities</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Retrieves a single entity by its identifier
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new entity in the database
    /// </summary>
    /// <param name="entity">The entity to create</param>
    /// <returns>The created entity with generated values</returns>
    Task<T> CreateAsync(T entity);

    /// <summary>
    /// Updates an existing entity in the database
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Deletes an entity from the database
    /// </summary>
    /// <param name="id">The identifier of the entity to delete</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task DeleteAsync(int id);
}
