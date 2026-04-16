/*
 * UserResponseDto.cs — Data Transfer Object for User API Responses
 *
 * Architectural Pattern: Data Transfer Object (DTO) — Response/Read variant
 *
 * WHY: Returns only the fields the client needs.
 * Excludes the Tasks navigation property (would require a separate endpoint with pagination).
 * TaskCount is included as a scalar — cheap JOIN COUNT, not loading all task objects.
 */

using TaskFlowAPI.Models;

namespace TaskFlowAPI.DTOs;

/// <summary>
/// The response shape for all user-related API endpoints.
/// Safe to serialize — no circular references or navigation properties.
/// </summary>
public class UserResponseDto
{
    /// <summary>User's unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>User's display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>User's email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Account creation timestamp in UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of tasks assigned to this user.
    /// Provides a useful summary for the UI without loading all task data.
    /// Computed in the service layer using LINQ .Count().
    /// </summary>
    public int TaskCount { get; set; }

    /// <summary>
    /// Factory method — converts a User entity to a DTO.
    /// </summary>
    /// <param name="user">The User entity, optionally with Tasks loaded.</param>
    /// <returns>A flattened, serialization-safe DTO.</returns>
    public static UserResponseDto FromEntity(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        CreatedAt = user.CreatedAt,
        // Tasks.Count is safe: ICollection always has a Count property
        // If Tasks wasn't loaded by EF (no .Include()), this returns 0 — which is fine
        // for contexts where we don't need the count
        TaskCount = user.Tasks?.Count ?? 0
    };
}
