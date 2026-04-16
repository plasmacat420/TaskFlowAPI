/*
 * UsersController.cs — Users REST API Controller
 *
 * Architectural Pattern: Controller (Presentation/API Layer)
 *
 * WHY: Same reasoning as TasksController — see that file for the full explanation.
 * This controller handles User CRUD operations at /api/users.
 */

using Microsoft.AspNetCore.Mvc;
using TaskFlowAPI.DTOs;
using TaskFlowAPI.Services;

namespace TaskFlowAPI.Controllers;

/// <summary>
/// REST API controller for user CRUD operations.
/// Base route: /api/users
/// </summary>
[ApiController]
[Route("api/[controller]")]  // → /api/users
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // ── GET /api/users ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all users with their task counts, ordered alphabetically.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    // ── GET /api/users/{id} ───────────────────────────────────────────────────

    /// <summary>
    /// Returns a single user by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    // ── POST /api/users ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new user.
    /// Returns 409 Conflict if the email is already registered.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var created = await _userService.CreateUserAsync(dto);
        if (created is null)
            // 409 Conflict: the resource already exists (email uniqueness violation)
            // WHY 409 not 400: 400 means "bad request format"; 409 means "valid request but conflict with current state"
            return Conflict("A user with this email address already exists.");

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // ── PUT /api/users/{id} ───────────────────────────────────────────────────

    /// <summary>
    /// Updates an existing user's name and/or email.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        var updated = await _userService.UpdateUserAsync(id, dto);
        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    // ── DELETE /api/users/{id} ────────────────────────────────────────────────

    /// <summary>
    /// Deletes a user. Their assigned tasks become unassigned (not deleted).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _userService.DeleteUserAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
