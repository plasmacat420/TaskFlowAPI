/*
 * TaskItem.cs — Domain Entity (Model Layer)
 *
 * Architectural Pattern: Anemic Domain Model / EF Core Entity
 *
 * WHY this pattern exists:
 * In a layered (N-tier) architecture, the Model layer holds plain data-structure classes
 * called POCOs (Plain Old CLR Objects). They have NO business logic — just properties.
 * Business rules live in the Service layer (separation of concerns).
 *
 * Entity Framework Core reads this class and maps it to a PostgreSQL table automatically:
 *   - Class name "TaskItem" → table "TaskItems"
 *   - Each property → a column with the matching data type
 *   - [Key] → PRIMARY KEY constraint
 *   - [Required] / [MaxLength] → NOT NULL / VARCHAR(n) constraints
 *
 * In a microservices architecture, this entity would live in a "Task Service"
 * and be the only service allowed to write to its own database (database-per-service pattern).
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlowAPI.Models;

/// <summary>
/// Represents a task item in the TaskFlow system.
/// EF Core maps this to the "TaskItems" table in PostgreSQL.
/// </summary>
public class TaskItem
{
    /// <summary>
    /// Primary key for the task.
    /// WHY Guid instead of int: Guid (UUID) IDs are safe for distributed systems.
    /// In a microservices setup, multiple service instances can generate unique IDs
    /// without coordinating with a central DB sequence — preventing race conditions.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Short title of the task.
    /// [Required] → NOT NULL in PostgreSQL. [MaxLength(200)] → VARCHAR(200).
    /// EF Core enforces these constraints both in migrations and at the model-validation level.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional detailed description of what needs to be done.
    /// The nullable type (string?) maps to a nullable TEXT column.
    /// string.Empty default is not used here because null means "no description provided".
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Current workflow state of the task (Pending → InProgress → Done).
    /// Stored as an integer in the DB (0, 1, 2) but exposed as a readable enum name in JSON.
    /// This avoids magic numbers in business logic and makes queries self-documenting.
    /// </summary>
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;

    /// <summary>
    /// Business priority of the task — drives UI sorting and filtering.
    /// Using an enum instead of a string prevents invalid values like "URGENT" or typos.
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>
    /// Optional due date for the task.
    /// DateTime? (nullable) maps to TIMESTAMPTZ in PostgreSQL.
    /// Always store in UTC (DateTime.UtcNow) — let the frontend convert to local time.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Immutable audit timestamp — set once when the task is created.
    /// WHY: Enterprise apps require audit trails for compliance, debugging, and analytics.
    /// This field is set in C# (not a DB default) so it's always UTC regardless of server timezone.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Foreign key — the ID of the User this task is assigned to.
    /// Nullable (Guid?) because tasks can exist without being assigned to anyone.
    /// EF Core uses this FK to build JOIN queries when the navigation property is included.
    /// </summary>
    [ForeignKey("AssignedUser")]
    public Guid? AssignedUserId { get; set; }

    /// <summary>
    /// Navigation property — EF Core populates this with the related User entity
    /// when .Include(t => t.AssignedUser) is used in a LINQ query (eager loading).
    ///
    /// WHY [JsonIgnore]: Without this, JSON serialization goes:
    ///   TaskItem → AssignedUser → Tasks → TaskItem → ... (infinite loop → stack overflow)
    /// [JsonIgnore] tells System.Text.Json to skip this property during serialization.
    /// We use a DTO (TaskResponseDto) to flatten the data safely for API responses.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public User? AssignedUser { get; set; }
}
