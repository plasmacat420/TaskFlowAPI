/*
 * UserService.cs — User Service Implementation (Business Logic Layer)
 *
 * Architectural Pattern: Service Layer (Concrete Implementation)
 *
 * WHY: Same reasoning as TaskService — see that file for the full explanation.
 * This service handles user business rules:
 * - Duplicate email prevention
 * - DTO ↔ Entity mapping
 */

using TaskFlowAPI.DTOs;
using TaskFlowAPI.Models;
using TaskFlowAPI.Repositories;

namespace TaskFlowAPI.Services;

/// <summary>
/// Implements user business logic, delegating data access to IUserRepository.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(UserResponseDto.FromEntity);
    }

    /// <inheritdoc />
    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user is null ? null : UserResponseDto.FromEntity(user);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Business rule: email must be unique across all users.
    /// This is enforced at two levels:
    /// 1. HERE in the service — returns null (→ 409 Conflict) if email is taken
    /// 2. At the DB level — unique index in AppDbContext catches race conditions
    ///    (two concurrent requests with the same email: one succeeds, one gets a DB exception)
    /// </remarks>
    public async Task<UserResponseDto?> CreateUserAsync(CreateUserDto dto)
    {
        // Check for existing user with same email (case-insensitive)
        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing is not null)
            return null;  // Controller returns 409 Conflict

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email.ToLowerInvariant()  // normalize to lowercase for consistency
        };

        var created = await _userRepository.CreateAsync(user);
        return UserResponseDto.FromEntity(created);
    }

    /// <inheritdoc />
    public async Task<UserResponseDto?> UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null) return null;

        // If email is being changed, check the new email isn't already taken by another user
        if (dto.Email is not null && dto.Email.ToLowerInvariant() != user.Email)
        {
            var emailTaken = await _userRepository.GetByEmailAsync(dto.Email);
            if (emailTaken is not null)
                return null;  // Controller returns 409 Conflict
        }

        // Apply partial updates
        if (dto.Name is not null) user.Name = dto.Name;
        if (dto.Email is not null) user.Email = dto.Email.ToLowerInvariant();

        var updated = await _userRepository.UpdateAsync(user);
        return UserResponseDto.FromEntity(updated);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(Guid id)
    {
        return await _userRepository.DeleteAsync(id);
    }
}
