/*
 * TaskRepository.cs — Task Repository Implementation (Data Access Layer)
 *
 * Architectural Pattern: Repository Pattern (Concrete Implementation)
 *
 * WHY this layer exists:
 * This is the ONLY file in the entire application that writes SQL (indirectly via LINQ/EF Core).
 * All database concerns — connection pooling, query building, entity tracking, transactions —
 * are isolated here. The Service layer above it is completely decoupled from EF Core.
 *
 * Benefits of this isolation:
 * - Replace PostgreSQL with another DB: only rewrite this file
 * - Test the service layer: inject a mock ITaskRepository (no DB needed)
 * - Optimize a query: change it here without touching business logic
 * - Add a caching layer: wrap this class or create a CachedTaskRepository implementing ITaskRepository
 *
 * LINQ to SQL translation examples (what EF Core generates):
 *   .Where(t => t.Status == status)   →  WHERE "Status" = @status
 *   .Include(t => t.AssignedUser)     →  LEFT JOIN "Users" ON "TaskItems"."AssignedUserId" = "Users"."Id"
 *   .OrderByDescending(t => t.CreatedAt) → ORDER BY "CreatedAt" DESC
 *   .ToListAsync()                    →  executes the query and returns results
 */

using Microsoft.EntityFrameworkCore;
using TaskFlowAPI.Data;
using TaskFlowAPI.Models;

namespace TaskFlowAPI.Repositories;

/// <summary>
/// PostgreSQL implementation of ITaskRepository using Entity Framework Core.
/// Registered in Program.cs as Scoped — one instance per HTTP request,
/// sharing the same AppDbContext (Unit of Work) for the duration of the request.
/// </summary>
public class TaskRepository : ITaskRepository
{
    /// <summary>
    /// The EF Core context — injected via constructor DI.
    /// WHY private readonly: ensures the context reference never changes after construction.
    /// The same DbContext instance is shared across all repositories in a single request
    /// (because DbContext is registered as Scoped in DI), enabling cross-repository transactions.
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// Constructor injection — ASP.NET Core DI calls this and provides the DbContext.
    /// This is the Dependency Injection pattern: dependencies are pushed in,
    /// not created inside the class (no `new AppDbContext()` here).
    /// Makes the class testable — pass a test double in unit tests.
    /// </summary>
    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    /// <remarks>
    /// .Include(t => t.AssignedUser) performs an eager load:
    /// EF Core generates a LEFT JOIN to load the User in the same query.
    /// Without .Include(), AssignedUser would be null (no lazy loading by default in EF Core 8).
    ///
    /// .AsNoTracking() tells EF Core not to track these entities in the change tracker.
    /// WHY: Read-only queries don't need tracking. Skipping tracking is faster (less memory,
    /// no snapshot comparison) and appropriate for GET endpoints.
    /// </remarks>
    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        return await _context.Tasks
            .Include(t => t.AssignedUser)  // eager load related User (LEFT JOIN)
            .AsNoTracking()                // skip change tracking for read-only query
            .OrderByDescending(t => t.CreatedAt)  // newest first
            .ToListAsync();                // execute query async, return as List
    }

    /// <inheritdoc />
    public async Task<TaskItem?> GetByIdAsync(Guid id)
    {
        return await _context.Tasks
            .Include(t => t.AssignedUser)
            .AsNoTracking()
            // FirstOrDefaultAsync → LIMIT 1 in SQL. Returns null if not found (no exception).
            // Using id directly is safe (parameterized query) — EF Core never interpolates
            // user input directly into SQL, preventing SQL injection by design.
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TaskItem>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Tasks
            .Include(t => t.AssignedUser)
            .AsNoTracking()
            // LINQ .Where() → SQL WHERE clause
            // EF Core translates the lambda to: WHERE "AssignedUserId" = @userId
            .Where(t => t.AssignedUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TaskItem>> GetByStatusAsync(TaskItemStatus status)
    {
        return await _context.Tasks
            .Include(t => t.AssignedUser)
            .AsNoTracking()
            // Enum comparison → WHERE "Status" = @status (integer value)
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.Priority)  // high priority first within status
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TaskItem>> GetByPriorityAsync(TaskPriority priority)
    {
        return await _context.Tasks
            .Include(t => t.AssignedUser)
            .AsNoTracking()
            .Where(t => t.Priority == priority)
            .OrderBy(t => t.Status)         // Pending first (0), then InProgress (1), then Done (2)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TaskItem> CreateAsync(TaskItem task)
    {
        // .AddAsync() queues the entity for INSERT — no SQL runs yet
        await _context.Tasks.AddAsync(task);
        // .SaveChangesAsync() executes the INSERT in a transaction and returns rows affected
        await _context.SaveChangesAsync();
        return task;  // entity now has any DB-generated values populated (e.g., auto-timestamps)
    }

    /// <inheritdoc />
    public async Task<TaskItem> UpdateAsync(TaskItem task)
    {
        // .Update() marks all properties of the entity as Modified
        // EF Core generates: UPDATE "TaskItems" SET col1=@val1, col2=@val2... WHERE "Id"=@id
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
        return task;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        // FindAsync uses the primary key — EF Core checks the change tracker first (cache)
        // before hitting the database. More efficient than FirstOrDefaultAsync for PK lookups.
        var task = await _context.Tasks.FindAsync(id);
        if (task is null) return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }
}
