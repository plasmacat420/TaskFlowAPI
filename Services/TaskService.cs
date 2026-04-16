/*
 * TaskService.cs — Task Service Implementation (Business Logic Layer)
 *
 * Architectural Pattern: Service Layer (Concrete Implementation)
 *
 * WHY this layer exists:
 * The Service layer is where business rules live. It:
 * 1. Validates inputs against business rules (not just format — actual domain rules)
 * 2. Orchestrates multiple repository calls within one logical operation
 * 3. Maps between DTOs and entities (the "translation layer")
 * 4. Returns DTOs to the Controller (the Controller knows nothing about entities)
 *
 * Key design decision: TaskService depends on BOTH ITaskRepository and IUserRepository.
 * This is intentional — business logic often spans multiple aggregate roots.
 * For example: "when assigning a task, verify the user exists" requires querying both.
 *
 * In a microservices system, cross-service validation (e.g., "does this userId exist?")
 * would be done via an HTTP call to the User Service, not a shared repository.
 * Here they share a DB but are accessed through separate repository interfaces.
 */

using TaskFlowAPI.DTOs;
using TaskFlowAPI.Models;
using TaskFlowAPI.Repositories;

namespace TaskFlowAPI.Services;

/// <summary>
/// Implements task business logic, delegating data access to the repository layer.
/// </summary>
public class TaskService : ITaskService
{
    /// <summary>
    /// Repository for task data access — injected via DI.
    /// The service doesn't know (or care) whether this is PostgreSQL, an in-memory store, or a mock.
    /// </summary>
    private readonly ITaskRepository _taskRepository;

    /// <summary>
    /// User repository — needed to validate that an assigned user exists.
    /// This cross-aggregate validation is a business rule, so it belongs in the service layer.
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Constructor injection — ASP.NET Core DI provides both repositories.
    /// Both are scoped — same instances are used throughout this HTTP request.
    /// They share the same AppDbContext, so any changes are transactional.
    /// </summary>
    public TaskService(ITaskRepository taskRepository, IUserRepository userRepository)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync()
    {
        var tasks = await _taskRepository.GetAllAsync();
        // LINQ .Select() transforms each TaskItem entity to a TaskResponseDto
        // This projection happens in memory (after DB query) — the mapping is C#, not SQL
        return tasks.Select(TaskResponseDto.FromEntity);
    }

    /// <inheritdoc />
    public async Task<TaskResponseDto?> GetTaskByIdAsync(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        // Null-conditional: if task is null, return null (Controller handles → 404)
        // If task exists, map it to a DTO
        return task is null ? null : TaskResponseDto.FromEntity(task);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TaskResponseDto>> GetTasksByUserAsync(Guid userId)
    {
        var tasks = await _taskRepository.GetByUserIdAsync(userId);
        return tasks.Select(TaskResponseDto.FromEntity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TaskResponseDto>> GetTasksByStatusAsync(TaskItemStatus status)
    {
        var tasks = await _taskRepository.GetByStatusAsync(status);
        return tasks.Select(TaskResponseDto.FromEntity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TaskResponseDto>> GetTasksByPriorityAsync(TaskPriority priority)
    {
        var tasks = await _taskRepository.GetByPriorityAsync(priority);
        return tasks.Select(TaskResponseDto.FromEntity);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Business rule enforced here: if AssignedUserId is provided, the user MUST exist.
    /// This is intentionally in the service layer, not the repository layer, because:
    /// - It's a business rule ("you can't assign to a phantom user"), not a data-access concern
    /// - It requires querying a different aggregate (User) to validate
    /// - It keeps the repository focused on single-entity CRUD
    /// </remarks>
    public async Task<TaskResponseDto?> CreateTaskAsync(CreateTaskDto dto)
    {
        // Business rule: validate the assigned user exists before creating the task
        if (dto.AssignedUserId.HasValue)
        {
            var userExists = await _userRepository.GetByIdAsync(dto.AssignedUserId.Value);
            if (userExists is null)
                return null;  // Controller returns 404 or 400 with a message
        }

        // Map DTO → Entity (manual mapping keeps it explicit and zero-dependency)
        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status,
            Priority = dto.Priority,
            DueDate = dto.DueDate,
            AssignedUserId = dto.AssignedUserId
            // Id and CreatedAt are set by the entity defaults — not from the DTO
        };

        var created = await _taskRepository.CreateAsync(task);

        // After creation, reload with the user to populate AssignedUserName in the response
        var withUser = await _taskRepository.GetByIdAsync(created.Id);
        return withUser is null ? null : TaskResponseDto.FromEntity(withUser);
    }

    /// <inheritdoc />
    public async Task<TaskResponseDto?> UpdateTaskAsync(Guid id, UpdateTaskDto dto)
    {
        // Fetch the existing entity — we need the current values to apply partial updates
        var task = await _taskRepository.GetByIdAsync(id);
        if (task is null) return null;

        // Validate new assigned user if provided
        if (dto.AssignedUserId.HasValue)
        {
            var userExists = await _userRepository.GetByIdAsync(dto.AssignedUserId.Value);
            if (userExists is null) return null;
        }

        // Apply updates from the DTO.
        // For value types (Status, Priority): only update when the client sends them (HasValue).
        // For nullable reference/value types that the client can explicitly CLEAR (Description,
        // DueDate, AssignedUserId): always assign — null from the client means "clear this field".
        // WHY: this is a full PUT from the Angular form which always sends every field.
        //      Using ??(null-coalesce) for these fields would prevent the user from ever
        //      unassigning a task or removing a due date, because null would be ignored.
        if (dto.Title is not null) task.Title = dto.Title;
        task.Description = dto.Description;          // null = "clear the description"
        if (dto.Status.HasValue)   task.Status   = dto.Status.Value;
        if (dto.Priority.HasValue) task.Priority = dto.Priority.Value;
        task.DueDate        = dto.DueDate;           // null = "remove the due date"
        task.AssignedUserId = dto.AssignedUserId;    // null = "unassign the task"

        // Note: AsNoTracking was used in the repository read, so we need to re-attach
        // EF Core will UPDATE all columns (not just changed ones) — acceptable for this app size
        var updated = await _taskRepository.UpdateAsync(task);

        // Reload with user for the response DTO
        var withUser = await _taskRepository.GetByIdAsync(updated.Id);
        return withUser is null ? null : TaskResponseDto.FromEntity(withUser);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTaskAsync(Guid id)
    {
        return await _taskRepository.DeleteAsync(id);
    }
}
