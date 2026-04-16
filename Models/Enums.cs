/*
 * Enums.cs — Domain Enumerations
 *
 * Architectural Pattern: Value Object (simplified enum form)
 *
 * WHY dedicated enums instead of magic strings:
 *
 * WITHOUT enums: Status = "InPorgress" (typo) compiles and ships to production.
 * WITH enums:    Status = TaskItemStatus.InPorgress → compile error caught instantly.
 *
 * Benefits in enterprise .NET:
 * 1. Type safety — impossible to set an invalid status value
 * 2. DB efficiency — stored as int (0,1,2) not varchar, saving storage and index space
 * 3. API clarity — Swagger renders enum names as string options in the docs
 * 4. LINQ-friendly — WHERE Status = 1 is faster than WHERE Status = 'InProgress'
 *
 * In a microservices/event-driven system, these enum definitions would live in a
 * shared "contracts" NuGet package consumed by all services that produce/consume task events.
 * This keeps the event schema consistent across service boundaries.
 *
 * NOTE: "TaskItemStatus" instead of "TaskStatus" — avoids collision with System.Threading.Tasks.TaskStatus.
 */

namespace TaskFlowAPI.Models;

/// <summary>
/// Represents the lifecycle state of a task.
/// Drives filtering, KPI calculations, and business workflow rules.
/// Stored as integer in PostgreSQL: Pending=0, InProgress=1, Done=2.
/// </summary>
public enum TaskItemStatus
{
    /// <summary>Task has been created but work has not started.</summary>
    Pending = 0,

    /// <summary>Task is actively being worked on.</summary>
    InProgress = 1,

    /// <summary>Task has been completed.</summary>
    Done = 2
}

/// <summary>
/// Represents the business priority of a task.
/// Used for visual indicators, sorting (High first), and SLA tracking.
/// Stored as integer in PostgreSQL: Low=0, Medium=1, High=2.
/// </summary>
public enum TaskPriority
{
    /// <summary>Nice-to-have, can be deferred.</summary>
    Low = 0,

    /// <summary>Standard work priority — the default.</summary>
    Medium = 1,

    /// <summary>Urgent — needs immediate attention.</summary>
    High = 2
}
