/*
 * UserRepository.cs — User Repository Implementation (Data Access Layer)
 *
 * Architectural Pattern: Repository Pattern (Concrete Implementation)
 *
 * WHY: Same reasoning as TaskRepository — see that file for the full explanation.
 * This file contains all PostgreSQL/EF Core user data access logic.
 */

using Microsoft.EntityFrameworkCore;
using TaskFlowAPI.Data;
using TaskFlowAPI.Models;

namespace TaskFlowAPI.Repositories;

/// <summary>
/// PostgreSQL implementation of IUserRepository using Entity Framework Core.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Includes Tasks for each user so the service can compute TaskCount.
    /// In a high-scale system you'd use a projected COUNT subquery instead of loading
    /// all task objects — but for this showcase, loading is clear and sufficient.
    /// </remarks>
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .Include(u => u.Tasks)   // eager load tasks for TaskCount calculation
            .AsNoTracking()
            .OrderBy(u => u.Name)    // alphabetical for consistent UI ordering
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.Tasks)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// .ToLowerInvariant() comparison: case-insensitive email lookup.
    /// WHY: "User@Example.com" and "user@example.com" are the same email.
    /// For production, store email lowercased and use a case-insensitive collation index.
    /// EF Core translates .ToLower() to LOWER() in SQL for PostgreSQL.
    /// </remarks>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    /// <inheritdoc />
    public async Task<User> CreateAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <inheritdoc />
    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The AppDbContext is configured with OnDelete: SetNull for the User → Task relationship.
    /// So deleting a user automatically sets AssignedUserId = NULL on their tasks
    /// (via a database-level CASCADE SET NULL). Tasks are preserved, just unassigned.
    /// </remarks>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
}
