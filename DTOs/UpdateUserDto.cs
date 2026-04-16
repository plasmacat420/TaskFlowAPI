/*
 * UpdateUserDto.cs — Data Transfer Object for User Updates
 *
 * Architectural Pattern: Data Transfer Object (DTO) — Update variant
 */

using System.ComponentModel.DataAnnotations;

namespace TaskFlowAPI.DTOs;

/// <summary>
/// The request body shape for PUT /api/users/{id}.
/// </summary>
public class UpdateUserDto
{
    /// <summary>Updated name — optional, only applied if provided.</summary>
    [MaxLength(100)]
    public string? Name { get; set; }

    /// <summary>Updated email — optional, validated for format if provided.</summary>
    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }
}
