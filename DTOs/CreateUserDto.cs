/*
 * CreateUserDto.cs — Data Transfer Object for User Creation
 *
 * Architectural Pattern: Data Transfer Object (DTO)
 *
 * WHY: Same reasoning as CreateTaskDto — prevents over-posting, decouples API from DB schema.
 * The client cannot set Id (auto-generated Guid) or CreatedAt (server-set UTC timestamp).
 */

using System.ComponentModel.DataAnnotations;

namespace TaskFlowAPI.DTOs;

/// <summary>
/// The request body shape for POST /api/users.
/// </summary>
public class CreateUserDto
{
    /// <summary>Full display name of the user.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address — validated for format by [EmailAddress] attribute.
    /// The service layer enforces uniqueness via a DB-level unique index.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;
}
