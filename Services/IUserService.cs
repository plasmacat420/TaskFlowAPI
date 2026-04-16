/*
 * IUserService.cs — User Service Interface (Business Logic Layer)
 *
 * Architectural Pattern: Service Layer (Interface/Contract)
 *
 * WHY: Same reasoning as ITaskService — see that file for the full explanation.
 */

using TaskFlowAPI.DTOs;

namespace TaskFlowAPI.Services;

/// <summary>
/// Contract for user business logic operations.
/// </summary>
public interface IUserService
{
    /// <summary>Gets all users as response DTOs, including their task counts.</summary>
    Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();

    /// <summary>
    /// Gets a user by ID.
    /// Returns null (→ 404) if not found.
    /// </summary>
    Task<UserResponseDto?> GetUserByIdAsync(Guid id);

    /// <summary>
    /// Creates a new user.
    /// Returns null if a user with the same email already exists (409 Conflict scenario).
    /// </summary>
    Task<UserResponseDto?> CreateUserAsync(CreateUserDto dto);

    /// <summary>
    /// Updates an existing user.
    /// Returns null if the user doesn't exist or the new email is already taken.
    /// </summary>
    Task<UserResponseDto?> UpdateUserAsync(Guid id, UpdateUserDto dto);

    /// <summary>
    /// Deletes a user by ID.
    /// Tasks assigned to this user become unassigned (AssignedUserId set to null via FK constraint).
    /// Returns false if user not found.
    /// </summary>
    Task<bool> DeleteUserAsync(Guid id);
}
