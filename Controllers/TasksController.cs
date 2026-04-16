/*
 * TasksController.cs — Tasks REST API Controller
 *
 * Architectural Pattern: Controller (Presentation/API Layer)
 *
 * WHY the Controller layer:
 * The Controller's only job is HTTP — it:
 * 1. Maps HTTP requests (method + route + body) to service method calls
 * 2. Maps service return values to HTTP responses (status codes + JSON body)
 * 3. Handles HTTP-level concerns: routing, model validation, authorization
 *
 * The Controller knows NOTHING about databases, SQL, or business rules.
 * It delegates everything to the Service layer.
 * This separation means you can add a gRPC endpoint, message queue consumer,
 * or CLI command that reuses the same Service layer without any changes.
 *
 * HTTP Status Code conventions used here:
 * - 200 OK         — successful GET or PUT (returned the resource)
 * - 201 Created    — successful POST (created a new resource, Location header optional)
 * - 204 No Content — successful DELETE (nothing to return)
 * - 400 Bad Request — validation failed (model state invalid or business rule violated)
 * - 404 Not Found  — resource doesn't exist
 * - 409 Conflict   — duplicate constraint violated (e.g., email already taken)
 */

using Microsoft.AspNetCore.Mvc;
using TaskFlowAPI.DTOs;
using TaskFlowAPI.Models;
using TaskFlowAPI.Services;

namespace TaskFlowAPI.Controllers;

/// <summary>
/// REST API controller for task CRUD and filtering operations.
/// Base route: /api/tasks
/// </summary>
[ApiController]
[Route("api/[controller]")]  // → /api/tasks
public class TasksController : ControllerBase
{
    /// <summary>
    /// The service layer — injected via DI.
    /// Controller only depends on the interface, never the concrete class.
    /// This makes the controller independently testable (inject a mock ITaskService in tests).
    /// </summary>
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    // ── GET /api/tasks ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all tasks, ordered by creation date (newest first).
    /// </summary>
    /// <returns>200 OK with an array of TaskResponseDto objects.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var tasks = await _taskService.GetAllTasksAsync();
        return Ok(tasks);  // 200 OK with JSON body
    }

    // ── GET /api/tasks/{id} ───────────────────────────────────────────────────

    /// <summary>
    /// Returns a single task by its ID.
    /// </summary>
    /// <param name="id">The task's Guid ID from the URL route.</param>
    /// <returns>200 OK with the task, or 404 Not Found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        // WHY null check: the service returns null for "not found" instead of throwing an exception.
        // Exceptions should be for exceptional cases (infrastructure failures), not expected business outcomes.
        return task is null ? NotFound() : Ok(task);
    }

    // ── GET /api/tasks/user/{userId} ──────────────────────────────────────────

    /// <summary>
    /// Returns all tasks assigned to a specific user.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TaskResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var tasks = await _taskService.GetTasksByUserAsync(userId);
        return Ok(tasks);
    }

    // ── GET /api/tasks/status/{status} ────────────────────────────────────────

    /// <summary>
    /// Returns all tasks with the specified workflow status.
    /// </summary>
    /// <param name="status">One of: Pending, InProgress, Done</param>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<TaskResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByStatus(string status)
    {
        // Parse the string route param to the enum — case-insensitive for usability
        if (!Enum.TryParse<TaskItemStatus>(status, ignoreCase: true, out var statusEnum))
            return BadRequest($"Invalid status '{status}'. Valid values: Pending, InProgress, Done");

        var tasks = await _taskService.GetTasksByStatusAsync(statusEnum);
        return Ok(tasks);
    }

    // ── GET /api/tasks/priority/{priority} ────────────────────────────────────

    /// <summary>
    /// Returns all tasks with the specified priority level.
    /// </summary>
    /// <param name="priority">One of: Low, Medium, High</param>
    [HttpGet("priority/{priority}")]
    [ProducesResponseType(typeof(IEnumerable<TaskResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByPriority(string priority)
    {
        if (!Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out var priorityEnum))
            return BadRequest($"Invalid priority '{priority}'. Valid values: Low, Medium, High");

        var tasks = await _taskService.GetTasksByPriorityAsync(priorityEnum);
        return Ok(tasks);
    }

    // ── POST /api/tasks ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="dto">The task creation data from the request body.</param>
    /// <returns>201 Created with the new task, or 400/404 if validation fails.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        // [ApiController] automatically returns 400 if model annotations fail ([Required], [MaxLength])
        // The service handles deeper business validation

        var created = await _taskService.CreateTaskAsync(dto);
        if (created is null)
            return BadRequest("Assigned user not found. Provide a valid AssignedUserId.");

        // 201 Created with Location header pointing to the new resource
        // CreatedAtAction generates: Location: /api/tasks/{id}
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // ── PUT /api/tasks/{id} ───────────────────────────────────────────────────

    /// <summary>
    /// Updates an existing task (partial update — only provided fields are changed).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskDto dto)
    {
        var updated = await _taskService.UpdateTaskAsync(id, dto);
        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    // ── DELETE /api/tasks/{id} ────────────────────────────────────────────────

    /// <summary>
    /// Deletes a task by ID.
    /// </summary>
    /// <returns>204 No Content on success, 404 Not Found if task doesn't exist.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _taskService.DeleteTaskAsync(id);
        // WHY 204 not 200: DELETE returns no body. 204 communicates "success but nothing to return".
        return deleted ? NoContent() : NotFound();
    }
}
