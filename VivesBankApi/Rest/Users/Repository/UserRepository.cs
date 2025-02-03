using Microsoft.EntityFrameworkCore;
using VivesBankApi.Database;
using VivesBankApi.Rest.Users.Models;

namespace VivesBankApi.Rest.Users.Repository;

/// <summary>
/// Repository for managing user-related database operations that extends the class <see cref="GenericRepository{C,T}"/>.
/// </summary>
public class UserRepository : GenericRepository<BancoDbContext, User>, IUserRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="context">The database context for user operations.</param>
    /// <param name="logger">Logger instance for logging repository actions.</param>
    public UserRepository(BancoDbContext context, ILogger<UserRepository> logger) : base(context, logger)
    {
    }

    /// <summary>
    /// Retrieves a user by their username (DNI).
    /// </summary>
    /// <param name="username">The username (DNI) of the user.</param>
    /// <returns>The user if found, otherwise null.</returns>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Dni == username);
    }

    /// <summary>
    /// Retrieves a paginated list of users based on filters and sorting options.
    /// </summary>
    /// <param name="pageNumber">The page number (zero-based).</param>
    /// <param name="pageSize">The number of users per page.</param>
    /// <param name="role">The user role filter.</param>
    /// <param name="isDeleted">Optional flag to filter deleted users.</param>
    /// <param name="direction">Sorting direction ("asc" or "desc") based on DNI.</param>
    /// <returns>A paged list of users matching the criteria.</returns>
    public async Task<PagedList<User>> GetAllUsersPagedAsync(
        int pageNumber,
        int pageSize,
        string role,
        bool? isDeleted,
        string direction)
    {
        _logger.LogInformation("Fetching all users");
        var query = _dbSet.AsQueryable();
        
        query = query.Where(a => a.Role.ToString().ToUpper().Contains(role.Trim().ToUpper()));
        
        if (isDeleted.HasValue)
        {
            query = query.Where(a => a.IsDeleted == isDeleted.Value);
        }
        
        query = direction.ToLower() switch
        {
            "desc" => query.OrderByDescending(e => e.Dni),
            _ => query.OrderBy(e => e.Dni)
        };
        
        query = query.Skip(pageNumber * pageSize).Take(pageSize);
        
        List<User> users = await query.ToListAsync();
        return new PagedList<User>(users, await _dbSet.CountAsync(), pageNumber, pageSize);
    }
}