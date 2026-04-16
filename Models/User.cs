/*
 * User.cs — Domain Entity (Model Layer)
 *
 * Architectural Pattern: Domain Entity
 *
 * WHY this pattern exists:
 * The User entity represents a person who can be assigned tasks.
 * It is a separate bounded context from TaskItem.
 *
 * In a real microservices architecture:
 *   - Users would live in an "Identity/Auth Service" (e.g., backed by IdentityServer or Auth0)
 *   - TaskItem would store only AssignedUserId (a reference, not a join)
 *   - The Task Service would call the User Service via HTTP/gRPC to resolve user details
 *
 * In this monolith showcase, both entities share one database but are logically separated
 * into their own repository and service layers — making future extraction straightforward.
 */

using System.ComponentModel.DataAnnotations;

namespace TaskFlowAPI.Models;

/// <summary>
/// Represents a system user who can be assigned tasks.
/// EF Core maps this to the "Users" table in PostgreSQL.
/// </summary>
public class User
{
    /// <summary>
    /// Primary key — Guid for distributed system compatibility.
    /// See TaskItem.Id comments for why Guid is preferred over int in enterprise systems.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Full display name of the user.
    /// [Required] enforces NOT NULL; [MaxLength(100)] creates VARCHAR(100) in PostgreSQL.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// User's email address — must be unique (enforced via index in AppDbContext).
    /// [EmailAddress] validates format at the model-binding level before hitting the DB.
    /// WHY unique constraint: prevents duplicate accounts, enables login-by-email flows.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Audit timestamp — when this user account was created.
    /// Stored in UTC, formatted by the frontend for the user's local timezone.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property — all tasks assigned to this user.
    ///
    /// WHY ICollection and not List:
    /// EF Core's change tracker may substitute its own internal collection type (e.g., HashSet)
    /// at runtime. Using the interface (ICollection) rather than a concrete type (List) lets EF
    /// do this transparently without breaking our code.
    ///
    /// [JsonIgnore] prevents circular serialization: User → Tasks → User → Tasks → ...
    /// API responses use UserResponseDto which excludes this collection entirely.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
