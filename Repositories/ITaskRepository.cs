/*
 * ITaskRepository.cs — Task Repository Interface (Repository Pattern)
 *
 * Architectural Pattern: Repository Pattern (Interface/Contract)
 *
 * WHY the Repository Pattern:
 * The repository is the only layer that "knows" about the database.
 * Every other layer (Service, Controller) depends on THIS INTERFACE, not the concrete class.
 *
 * This enables:
 * 1. Testability: swap the real PostgreSQL repository with an in-memory fake in unit tests
 *    without changing a single line of service or controller code.
 * 2. Separation of concerns: business logic (Service layer) stays clean — no SQL/EF code.
 * 3. Replaceability: if you switch from PostgreSQL to MongoDB, only the concrete
 *    repository class changes. The interface and everything above it stays the same.
 *
 * This is the Dependency Inversion Principle (the D in SOLID):
 * "High-level modules should not depend on low-level modules. Both should depend on abstractions."
 * The Service layer (high-level) depends on ITaskRepository (abstraction), not TaskRepository (low-level).
 *
 * In a microservices architecture, the repository pattern also makes it easier to introduce
 * a caching layer (Redis) between the service and DB without changing the interface contract.
 */

using TaskFlowAPI.Models;

namespace TaskFlowAPI.Repositories;

/// <summary>
/// Contract for all task data-access operations.
/// Implemented by TaskRepository (PostgreSQL) — and potentially by a mock in tests.
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Retrieves all tasks, with their assigned user data loaded.
    /// WHY async: database I/O is a blocking operation. async/await frees the thread
    /// to handle other requests while waiting for the DB response (non-blocking I/O).
    /// In a high-throughput API, this can dramatically increase requests-per-second.
    /// </summary>
    /// <returns>All tasks including their assigned user details.</returns>
    Task<IEnumerable<TaskItem>> GetAllAsync();

    /// <summary>
    /// Retrieves a single task by its unique ID, including the assigned user.
    /// Returns null if no task with that ID exists (not an exception — absence is a valid state).
    /// </summary>
    /// <param name="id">The task's Guid primary key.</param>
    /// <returns>The task or null if not found.</returns>
    Task<TaskItem?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all tasks assigned to a specific user.
    /// Used for the "My Tasks" view in the UI.
    /// </summary>
    /// <param name="userId">The user's Guid.</param>
    Task<IEnumerable<TaskItem>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Retrieves all tasks matching a specific workflow status.
    /// Used for Kanban-style filtering (show only Pending, InProgress, or Done).
    /// </summary>
    /// <param name="status">The TaskItemStatus enum value to filter by.</param>
    Task<IEnumerable<TaskItem>> GetByStatusAsync(TaskItemStatus status);

    /// <summary>
    /// Retrieves all tasks matching a specific priority level.
    /// Used for priority-based filtering and sorting.
    /// </summary>
    /// <param name="priority">The TaskPriority enum value to filter by.</param>
    Task<IEnumerable<TaskItem>> GetByPriorityAsync(TaskPriority priority);

    /// <summary>
    /// Adds a new task to the database.
    /// </summary>
    /// <param name="task">The new TaskItem entity to persist.</param>
    /// <returns>The created task with any DB-generated values populated.</returns>
    Task<TaskItem> CreateAsync(TaskItem task);

    /// <summary>
    /// Updates an existing task in the database.
    /// </summary>
    /// <param name="task">The modified TaskItem entity.</param>
    /// <returns>The updated task.</returns>
    Task<TaskItem> UpdateAsync(TaskItem task);

    /// <summary>
    /// Deletes a task by ID.
    /// </summary>
    /// <param name="id">The ID of the task to delete.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id);
}
