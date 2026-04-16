/*
 * UpdateTaskDto.cs — Data Transfer Object for Task Updates
 *
 * Architectural Pattern: Data Transfer Object (DTO) — Update variant
 *
 * WHY a separate DTO for updates:
 * Update requests often differ from create requests:
 * - Id comes from the URL route, not the body (prevents ID substitution attacks)
 * - CreatedAt must never change (immutable audit field)
 * - All fields are optional — client only sends what changed (partial update pattern)
 *
 * This mirrors the PUT/PATCH semantics:
 * - PUT: replace the entire resource (all fields required)
 * - PATCH: update only changed fields (all fields optional)
 * We use PUT here with all fields optional for simplicity, which works for this showcase.
 */

using System.ComponentModel.DataAnnotations;
using TaskFlowAPI.Models;

namespace TaskFlowAPI.DTOs;

/// <summary>
/// The request body shape for PUT /api/tasks/{id}.
/// All fields are optional — only provided fields will be applied.
/// </summary>
public class UpdateTaskDto
{
    /// <summary>Updated title — if provided, replaces the existing title.</summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>Updated description — if provided, replaces the existing description.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Updated status — used to advance the task through its lifecycle.</summary>
    public TaskItemStatus? Status { get; set; }

    /// <summary>Updated priority — can be escalated or de-escalated by the user.</summary>
    public TaskPriority? Priority { get; set; }

    /// <summary>Updated due date — pass null to remove an existing due date.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Updated assigned user — pass null to unassign the task.</summary>
    public Guid? AssignedUserId { get; set; }
}
