/*
 * AppDbContext.cs — Database Context (Data Access Layer)
 *
 * Architectural Pattern: Unit of Work + Repository support via EF Core
 *
 * WHY DbContext:
 * DbContext is the central EF Core class that:
 * 1. Holds the connection to PostgreSQL
 * 2. Tracks in-memory changes to entities (Change Tracker)
 * 3. Translates LINQ queries to SQL via the provider (Npgsql)
 * 4. Applies schema configuration (column types, indexes, constraints)
 * 5. Manages transactions (SaveChangesAsync wraps all pending changes in one transaction)
 *
 * DbContext IS the "Unit of Work" pattern from DDD:
 * - Multiple repository operations within one HTTP request share the same DbContext instance
 * - All changes are committed or rolled back together via SaveChangesAsync
 * - This is why DbContext is registered with Scoped lifetime (one per HTTP request)
 *
 * In a microservices system, each service has its own DbContext and its own database.
 * Cross-service data consistency is handled via eventual consistency / Saga patterns,
 * not distributed transactions (two-phase commit is fragile and slow at scale).
 */

using Microsoft.EntityFrameworkCore;
using TaskFlowAPI.Models;

namespace TaskFlowAPI.Data;

/// <summary>
/// The EF Core database context for TaskFlow.
/// Registered in Program.cs with Scoped lifetime — one instance per HTTP request.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Constructor accepts DbContextOptions (injected by DI).
    /// This pattern allows the connection string and provider to be configured
    /// externally (in Program.cs) — enabling easy switching between providers
    /// (PostgreSQL in production, SQLite in tests) without changing this class.
    /// This is the Dependency Inversion Principle: high-level code (DbContext)
    /// doesn't depend on low-level details (specific DB provider configuration).
    /// </summary>
    /// <param name="options">EF Core options including the DB provider and connection string.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// DbSet represents a table in the database.
    /// LINQ queries against this property are translated to SQL by EF Core.
    /// Example: context.Tasks.Where(t => t.Status == TaskItemStatus.Pending) →
    ///          SELECT * FROM "TaskItems" WHERE "Status" = 0
    /// </summary>
    public DbSet<TaskItem> Tasks { get; set; }

    /// <summary>
    /// DbSet for the Users table.
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Fluent API configuration — called by EF Core when building the model.
    /// This is where we define constraints, indexes, and relationships
    /// that can't be expressed via data annotations on the entity alone.
    ///
    /// WHY Fluent API over annotations:
    /// - More powerful (can configure composite keys, owned types, table splitting)
    /// - Keeps entity classes clean (no infrastructure attributes on domain objects)
    /// - Easier to review in one place — all DB configuration here
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder DSL.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── TaskItem configuration ────────────────────────────────────────────

        modelBuilder.Entity<TaskItem>(entity =>
        {
            // Map to a clearly named table (EF default would use DbSet property name "Tasks")
            entity.ToTable("TaskItems");

            // Enum columns: store as integer in PostgreSQL for performance
            // EF Core handles int↔enum conversion automatically via .HasConversion()
            entity.Property(t => t.Status)
                  .HasConversion<int>();

            entity.Property(t => t.Priority)
                  .HasConversion<int>();

            // Index on Status — common filter in queries like "get all pending tasks"
            // Without an index, PostgreSQL does a full table scan (O(n))
            // With an index, it's O(log n) — critical as task count grows
            entity.HasIndex(t => t.Status)
                  .HasDatabaseName("IX_TaskItems_Status");

            // Index on AssignedUserId — used by "get tasks by user" query
            entity.HasIndex(t => t.AssignedUserId)
                  .HasDatabaseName("IX_TaskItems_AssignedUserId");

            // Relationship: TaskItem has one optional User (AssignedUser)
            // FK: TaskItem.AssignedUserId → User.Id
            // OnDelete: SetNull — if a user is deleted, tasks become unassigned (not cascade-deleted)
            entity.HasOne(t => t.AssignedUser)
                  .WithMany(u => u.Tasks)
                  .HasForeignKey(t => t.AssignedUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── User configuration ────────────────────────────────────────────────

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            // Unique index on Email — prevents duplicate accounts
            // [EmailAddress] validates format; this enforces uniqueness at the DB level
            entity.HasIndex(u => u.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_Email_Unique");
        });
    }
}
