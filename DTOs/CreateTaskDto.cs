/*
 * CreateTaskDto.cs — Data Transfer Object for Task Creation
 *
 * Architectural Pattern: Data Transfer Object (DTO)
 *
 * WHY DTOs instead of exposing the entity directly:
 *
 * Problem with exposing entities directly in API endpoints:
 * 1. Over-posting attacks — a client could set CreatedAt, Id, or AssignedUser directly
 * 2. Circular reference issues — EF navigation properties cause infinite JSON loops
 * 3. Coupling — API contract changes every time the DB schema changes
 * 4. Versioning — hard to evolve the API independently from the database
 *
 * DTOs define an explicit contract between the API and its consumers.
 * The Controller maps incoming DTOs to entities (or uses AutoMapper in larger projects).
 *
 * This is the Interface Segregation Principle: CreateTaskDto only has what's needed
 * for creation — no Id (auto-generated), no CreatedAt (server-set), no navigation properties.
 */

using System.ComponentModel.DataAnnotations;
using TaskFlowAPI.Models;

namespace TaskFlowAPI.DTOs;

/// <summary>
/// The request body shape for POST /api/tasks.
/// Only contains fields the client is allowed to set on creation.
/// </summary>
public class CreateTaskDto
{
    /// <summary>Required task title — validated before reaching the service layer.</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description — client may omit this field entirely.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Initial status — defaults to Pending if not provided.
    /// The client can optionally create a task as InProgress (e.g., backlog import).
    /// </summary>
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;

    /// <summary>Priority level — defaults to Medium.</summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>Optional due date in UTC.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Optional user ID to assign the task to on creation.</summary>
    public Guid? AssignedUserId { get; set; }
}
