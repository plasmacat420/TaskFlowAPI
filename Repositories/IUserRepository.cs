/*
 * IUserRepository.cs — User Repository Interface (Repository Pattern)
 *
 * Architectural Pattern: Repository Pattern (Interface/Contract)
 *
 * WHY: Same reasoning as ITaskRepository — see that file for the full explanation.
 * This interface defines the contract for user data access operations.
 * The service layer depends on this interface, not the concrete implementation.
 */

using TaskFlowAPI.Models;

namespace TaskFlowAPI.Repositories;

/// <summary>
/// Contract for all user data-access operations.
/// Implemented by UserRepository — mockable for unit testing.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves all users, including a count of their assigned tasks.
    /// WHY include tasks: the UI shows task count per user without a separate API call.
    /// </summary>
    Task<IEnumerable<User>> GetAllAsync();

    /// <summary>
    /// Retrieves a single user by ID, including their assigned tasks.
    /// Returns null if not found.
    /// </summary>
    /// <param name="id">The user's Guid primary key.</param>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a user by their email address.
    /// Used for duplicate-check before creating a new user.
    /// Returns null if no user has that email.
    /// </summary>
    /// <param name="email">The email address to look up.</param>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Creates a new user in the database.
    /// </summary>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task<User> UpdateAsync(User user);

    /// <summary>
    /// Deletes a user by ID.
    /// Due to the SetNull FK constraint (configured in AppDbContext),
    /// deleting a user sets AssignedUserId to null on all their tasks — not deletes the tasks.
    /// </summary>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id);
}
