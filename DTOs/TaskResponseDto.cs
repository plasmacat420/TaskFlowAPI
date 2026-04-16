/*
 * TaskResponseDto.cs — Data Transfer Object for Task API Responses
 *
 * Architectural Pattern: Data Transfer Object (DTO) — Response/Read variant
 *
 * WHY a dedicated response DTO:
 * The response shape is what the API consumer (Angular UI) depends on.
 * By using a DTO instead of returning the entity directly, we:
 * 1. Flatten the object graph — no nested navigation properties, no circular refs
 * 2. Project only what the client needs — AssignedUserName (string) not full User object
 * 3. Decouple the API contract from the DB schema — schema can change without breaking clients
 * 4. Control serialization — no [JsonIgnore] hacks needed on the entity
 *
 * In larger projects this mapping is done with AutoMapper. Here we map manually
 * in the service layer to keep dependencies minimal and the mapping explicit/visible.
 */

using TaskFlowAPI.Models;

namespace TaskFlowAPI.DTOs;

/// <summary>
/// The response shape for all task-related API endpoints (GET, POST, PUT).
/// Flattened, safe to serialize — no circular references.
/// </summary>
public class TaskResponseDto
{
    /// <summary>Task's unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Task title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Task description — null if not set.</summary>
    public string? Description { get; set; }

    /// <summary>Current workflow status as a readable string (e.g., "InProgress").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Priority level as a readable string (e.g., "High").</summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>Optional due date in UTC.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Creation timestamp in UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>ID of the assigned user — null if unassigned.</summary>
    public Guid? AssignedUserId { get; set; }

    /// <summary>
    /// Display name of the assigned user — flattened from the User navigation property.
    /// WHY flatten: the client only needs to display a name, not make another API call.
    /// This is the "N+1 query prevention" pattern — we include the user name in the same query.
    /// </summary>
    public string? AssignedUserName { get; set; }

    /// <summary>
    /// Factory method — converts a TaskItem entity (with optional loaded User) to a DTO.
    /// WHY static factory on DTO: keeps mapping logic co-located with the DTO definition,
    /// easy to find and update when the DTO shape changes.
    /// </summary>
    /// <param name="task">The TaskItem entity, optionally with AssignedUser loaded.</param>
    /// <returns>A flattened, serialization-safe DTO.</returns>
    public static TaskResponseDto FromEntity(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        // .ToString() on an enum returns the name ("InProgress"), not the number (1)
        Status = task.Status.ToString(),
        Priority = task.Priority.ToString(),
        DueDate = task.DueDate,
        CreatedAt = task.CreatedAt,
        AssignedUserId = task.AssignedUserId,
        AssignedUserName = task.AssignedUser?.Name  // null-conditional: safe if not loaded
    };
}
