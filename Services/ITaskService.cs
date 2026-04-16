/*
 * ITaskService.cs — Task Service Interface (Business Logic Layer)
 *
 * Architectural Pattern: Service Layer (Interface/Contract)
 *
 * WHY the Service Layer:
 * The Service layer sits between Controllers and Repositories.
 * It is responsible for business logic — rules about WHAT can happen, not HOW data is stored.
 *
 * Examples of business logic that lives here (not in the Controller or Repository):
 * - "A user must exist before assigning a task to them"
 * - "A Done task cannot be moved back to Pending"
 * - "Notify the user when a High-priority task is assigned to them"
 *
 * WHY the interface:
 * Controllers depend on ITaskService (the abstraction), not TaskService (the concrete class).
 * This allows:
 * - Unit testing controllers by injecting a mock ITaskService
 * - Swapping implementations (e.g., a TaskServiceWithNotifications that wraps the original)
 * - Clean dependency graph visible at a glance
 *
 * The Service layer works with DTOs at its boundary — it maps DTOs to/from entities,
 * shielding the Controller from entity details.
 */

using TaskFlowAPI.DTOs;
using TaskFlowAPI.Models;

namespace TaskFlowAPI.Services;

/// <summary>
/// Contract for task business logic operations.
/// Implemented by TaskService — injectable and testable.
/// </summary>
public interface ITaskService
{
    /// <summary>Gets all tasks as response DTOs.</summary>
    Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync();

    /// <summary>
    /// Gets a single task by ID.
    /// Returns null if not found — the Controller translates null to 404 Not Found.
    /// </summary>
    Task<TaskResponseDto?> GetTaskByIdAsync(Guid id);

    /// <summary>Gets all tasks assigned to a specific user.</summary>
    Task<IEnumerable<TaskResponseDto>> GetTasksByUserAsync(Guid userId);

    /// <summary>Gets all tasks with a specific workflow status.</summary>
    Task<IEnumerable<TaskResponseDto>> GetTasksByStatusAsync(TaskItemStatus status);

    /// <summary>Gets all tasks with a specific priority.</summary>
    Task<IEnumerable<TaskResponseDto>> GetTasksByPriorityAsync(TaskPriority priority);

    /// <summary>
    /// Creates a new task from the provided DTO.
    /// Validates that AssignedUserId (if provided) refers to an existing user.
    /// Returns null if validation fails (e.g., user not found).
    /// </summary>
    Task<TaskResponseDto?> CreateTaskAsync(CreateTaskDto dto);

    /// <summary>
    /// Updates an existing task.
    /// Returns null if the task or assigned user doesn't exist.
    /// </summary>
    Task<TaskResponseDto?> UpdateTaskAsync(Guid id, UpdateTaskDto dto);

    /// <summary>
    /// Deletes a task by ID.
    /// Returns false if the task was not found.
    /// </summary>
    Task<bool> DeleteTaskAsync(Guid id);
}
