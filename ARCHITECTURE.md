# TaskFlow — Architecture Guide

A complete walkthrough of every architectural decision in this project.
Designed to let you explain the codebase confidently in a technical interview.

---

## Full System Diagram

```
┌───────────────────────────────────────────────────────────────────────────┐
│                          USER'S BROWSER                                   │
│                                                                           │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    Angular SPA (GitHub Pages)                       │  │
│  │                                                                     │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────┐  │  │
│  │  │  Dashboard   │  │  Task List   │  │  Task Form   │  │ Users  │  │  │
│  │  │  Component   │  │  Component   │  │  Component   │  │  List  │  │  │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └───┬────┘  │  │
│  │         │                 │                 │              │       │  │
│  │         └─────────────────┴─────────────────┴──────────────┘       │  │
│  │                           │                                         │  │
│  │                  ┌────────┴────────┐                                │  │
│  │                  │  Angular        │                                │  │
│  │                  │  Services       │  TaskService / UserService     │  │
│  │                  │  (HttpClient)   │                                │  │
│  │                  └────────┬────────┘                                │  │
│  └───────────────────────────┼─────────────────────────────────────────┘  │
│                              │ HTTP/JSON (CORS)                           │
└──────────────────────────────┼────────────────────────────────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │   Render (Docker)   │
                    │                     │
                    │   .NET 8 Web API    │
                    │                     │
                    │  ┌───────────────┐  │
                    │  │  Controllers  │  │  HTTP layer
                    │  └───────┬───────┘  │
                    │          │          │
                    │  ┌───────▼───────┐  │
                    │  │   Services    │  │  Business logic
                    │  └───────┬───────┘  │
                    │          │          │
                    │  ┌───────▼───────┐  │
                    │  │ Repositories  │  │  Data access (LINQ/EF Core)
                    │  └───────┬───────┘  │
                    │          │          │
                    │  ┌───────▼───────┐  │
                    │  │  AppDbContext │  │  EF Core Unit of Work
                    │  └───────┬───────┘  │
                    └──────────┼──────────┘
                               │ Npgsql / SQL
                    ┌──────────▼──────────┐
                    │   PostgreSQL DB     │
                    │   (Render managed)  │
                    │                     │
                    │  ┌───────────────┐  │
                    │  │   TaskItems   │  │
                    │  │   Users       │  │
                    │  └───────────────┘  │
                    └─────────────────────┘
```

---

## Backend Layers — Why Each Exists

### 1. Model Layer (`Models/`)

**Files:** `TaskItem.cs`, `User.cs`, `Enums.cs`

**What:** Plain C# classes (POCOs) that map 1:1 to database tables.

**Why:**
- EF Core reads these classes to build the database schema via migrations.
- Keeping models as pure data structures (no business logic) is the Single Responsibility Principle.
- The `[Key]`, `[Required]`, `[MaxLength]` attributes define database constraints declaratively.

**Interview talking points:**
- Why Guid instead of int for IDs? Distributed systems safety — no central sequence coordinator needed.
- Why `string?` (nullable) for Description? Maps to NULL in PostgreSQL; communicates "optional" at compile time.
- Why `[JsonIgnore]` on navigation properties? Prevents infinite JSON serialization loops.

---

### 2. DTO Layer (`DTOs/`)

**Files:** `CreateTaskDto`, `UpdateTaskDto`, `TaskResponseDto`, `CreateUserDto`, `UpdateUserDto`, `UserResponseDto`

**What:** Data Transfer Objects — the API's public contract with consumers.

**Why:**
- **Security:** Prevents over-posting. Without DTOs, a client could set `CreatedAt` or `Id` directly on the entity.
- **Decoupling:** The API contract can evolve independently from the DB schema.
- **Safety:** Response DTOs flatten the object graph, avoiding circular reference serialization issues.
- **Clarity:** `CreateTaskDto` makes it explicit exactly what fields are accepted on creation.

**Interview talking points:**
- DTOs vs AutoMapper: AutoMapper reduces mapping boilerplate but adds a dependency and magic. Manual mapping (as done here) is explicit and easy to debug.
- Factory methods on DTOs (`TaskResponseDto.FromEntity()`): co-locates mapping logic with the DTO definition.

---

### 3. Data Layer (`Data/`)

**Files:** `AppDbContext.cs`

**What:** The EF Core `DbContext` — connection, schema configuration, and Unit of Work.

**Why:**
- `DbSet<T>` properties represent tables — LINQ queries against them generate SQL.
- `OnModelCreating` uses Fluent API to configure indexes, constraints, and relationships that can't be expressed via annotations alone.
- Registered as **Scoped** in DI — one instance per HTTP request — enabling the Unit of Work pattern.

**Key configurations:**
```csharp
// Index for fast "get tasks by status" queries
entity.HasIndex(t => t.Status).HasDatabaseName("IX_TaskItems_Status");

// OnDelete: SetNull — deleting a user unassigns their tasks (doesn't delete them)
entity.HasOne(t => t.AssignedUser)
      .WithMany(u => u.Tasks)
      .HasForeignKey(t => t.AssignedUserId)
      .OnDelete(DeleteBehavior.SetNull);
```

**Interview talking points:**
- Unit of Work: multiple repository operations in one HTTP request share one DbContext. All changes commit/rollback together via `SaveChangesAsync()`.
- Why Scoped (not Singleton) for DbContext: DbContext is stateful (tracks entity changes). A Singleton would accumulate stale tracked entities across requests.
- EF Core migrations: version-controlled incremental schema changes. `db.Database.Migrate()` on startup applies pending migrations automatically on Render.

---

### 4. Repository Layer (`Repositories/`)

**Files:** `ITaskRepository.cs`, `TaskRepository.cs`, `IUserRepository.cs`, `UserRepository.cs`

**What:** The only layer that writes SQL (indirectly via LINQ/EF Core). Every data access operation is here.

**Why the Repository Pattern:**
```
Without Repository:
  Service → DbContext (tight coupling to EF Core everywhere)
  
With Repository:
  Service → ITaskRepository (interface — no EF Core dependency)
              ↓ (implemented by)
           TaskRepository → DbContext
```

**Benefits:**
1. Swap implementations: replace PostgreSQL with another store by writing a new class implementing the same interface.
2. Test the service layer with a fake/mock ITaskRepository — no database needed in unit tests.
3. Add caching: create `CachedTaskRepository : ITaskRepository` that wraps `TaskRepository`.

**Key LINQ patterns used:**
```csharp
.Include(t => t.AssignedUser)     // Eager load → LEFT JOIN
.AsNoTracking()                   // Skip change tracking for read-only queries
.Where(t => t.Status == status)   // Filter → WHERE clause
.OrderByDescending(t => t.CreatedAt) // Sort → ORDER BY DESC
.ToListAsync()                    // Execute query asynchronously
```

**Interview talking points:**
- `AsNoTracking()`: for read-only GET queries, skipping the change tracker is faster (less memory, no snapshot).
- `Include()` vs lazy loading: EF Core 8 defaults to no lazy loading. Explicit `.Include()` makes it clear which data is loaded.
- N+1 query problem: without `.Include(t => t.AssignedUser)`, accessing `task.AssignedUser` in a loop would fire one SQL query per task.

---

### 5. Service Layer (`Services/`)

**Files:** `ITaskService.cs`, `TaskService.cs`, `IUserService.cs`, `UserService.cs`

**What:** Business logic — validation, orchestration, DTO↔Entity mapping.

**Why:**
- Keeps Controllers thin: controllers handle HTTP, services handle "can this happen?"
- Business rules that span multiple entities live here (e.g., "verify user exists before assigning task")
- Isolated from both HTTP concerns (no `HttpRequest`) and DB concerns (no SQL/EF)

**Example business rule in TaskService:**
```csharp
// "Can't assign a task to a user that doesn't exist"
if (dto.AssignedUserId.HasValue) {
    var userExists = await _userRepository.GetByIdAsync(dto.AssignedUserId.Value);
    if (userExists is null) return null;  // → 400 Bad Request
}
```

**Interview talking points:**
- Service coordinates two repositories (task + user) — this is appropriate in a monolith.
- In microservices, cross-service validation is done via HTTP calls (UserService API), not shared repositories.
- `async/await` throughout: all I/O is non-blocking. The thread is free to handle other requests while waiting for PostgreSQL.

---

### 6. Controller Layer (`Controllers/`)

**Files:** `TasksController.cs`, `UsersController.cs`

**What:** Maps HTTP requests to service calls; maps return values to HTTP responses.

**Why the Controller is thin:**
```
Controller knows:   HTTP (routes, verbs, status codes, JSON)
Controller ignores: SQL, EF Core, business rules, entity internals
```

**HTTP Status Code semantics:**
| Code | Meaning | When used |
|------|---------|-----------|
| 200 OK | Success with body | GET, PUT returns resource |
| 201 Created | New resource created | POST success |
| 204 No Content | Success, no body | DELETE success |
| 400 Bad Request | Validation failed | Invalid input |
| 404 Not Found | Resource doesn't exist | Get/update/delete missing item |
| 409 Conflict | Duplicate constraint | Email already exists |

**Interview talking points:**
- `[ApiController]` attribute: automatically validates model annotations and returns 400 if invalid. No need for `if (!ModelState.IsValid)` checks.
- `[ProducesResponseType]` attributes: tell Swagger what status codes this endpoint can return, enabling accurate API documentation.
- `CreatedAtAction(nameof(GetById), ...)`: sets the `Location` header on 201 responses to the URL of the created resource. REST best practice.

---

## Dependency Injection Flow

```
HTTP Request arrives at TasksController.Create()
         │
         ▼
ASP.NET Core DI container:
  1. Creates AppDbContext (Scoped — new for this request)
  2. Creates TaskRepository (Scoped) → injects DbContext
  3. Creates UserRepository (Scoped) → injects DbContext (SAME instance)
  4. Creates TaskService (Scoped) → injects TaskRepository + UserRepository
  5. Creates TasksController → injects TaskService
         │
         ▼
Request handled → Response sent → ALL Scoped objects disposed
```

**WHY this matters:**
- The controller never calls `new TaskService()` — it's pushed in.
- Swapping `TaskRepository` for `FakeTaskRepository` in tests requires changing ONE line (in Program.cs or test setup).
- All objects in one request share the same DbContext → transactions work correctly.

---

## Data Flow: HTTP Request → Database → Response

```
POST /api/tasks
  Body: { "title": "Fix bug #123", "priority": "High", "assignedUserId": "abc-..." }

1. CONTROLLER (TasksController.Create)
   ├── [ApiController] validates CreateTaskDto annotations (Title required, MaxLength, etc.)
   ├── Calls: taskService.CreateTaskAsync(dto)
   └── Maps result to HTTP 201 Created

2. SERVICE (TaskService.CreateTaskAsync)
   ├── Checks AssignedUserId exists via userRepository.GetByIdAsync()
   ├── Maps CreateTaskDto → TaskItem entity
   └── Calls: taskRepository.CreateAsync(task)

3. REPOSITORY (TaskRepository.CreateAsync)
   ├── context.Tasks.AddAsync(task)  → queues INSERT
   ├── context.SaveChangesAsync()    → executes INSERT in transaction
   └── Returns saved entity

4. EF CORE → NPGSQL → POSTGRESQL
   SQL: INSERT INTO "TaskItems" ("Id","Title","Priority",...) VALUES (@p1,@p2,@p3,...)

5. RESPONSE (reverse path)
   Repository → Service (reloads with JOIN for AssignedUserName)
   Service → TaskResponseDto.FromEntity(task) → DTO
   Controller → CreatedAtAction → HTTP 201 + JSON body
```

---

## How This Maps to Microservices

This application is a **modular monolith** — the code is structured as if it were microservices, but deployed as one unit. This is intentional: it demonstrates the patterns used in microservices without the operational complexity.

| This App | Microservices Equivalent |
|----------|-------------------------|
| `TaskRepository` | Task Service (owns TaskItems table) |
| `UserRepository` | Identity Service (owns Users table) |
| Cross-repository validation in TaskService | HTTP call from Task Service to Identity Service |
| Shared AppDbContext | Separate databases (database-per-service) |
| `ITaskRepository` interface | Service contract (e.g., gRPC proto or OpenAPI spec) |
| Enum values in Enums.cs | Shared contracts NuGet package |
| EF Core migrations | Flyway/Liquibase per service |
| Render web service | Kubernetes pod / ECS task |

**Interview answer to "How would you extract this into microservices?":**
1. Split into Task Service (C# API) and User Service (C# API)
2. Give each service its own PostgreSQL database
3. Replace `userRepository.GetByIdAsync()` in TaskService with an HTTP call to User Service
4. Use a message broker (RabbitMQ/Kafka) for async events (e.g., "UserDeleted" → Task Service sets AssignedUserId = null)
5. Deploy each as a separate Docker container behind a Kubernetes ingress or API Gateway

---

## Frontend Architecture

```
app.module.ts (NgModule — DI container + component registry)
│
├── Routing (app-routing.module.ts)
│   /dashboard  → DashboardComponent
│   /tasks      → TaskListComponent
│   /tasks/new  → TaskFormComponent
│   /tasks/edit/:id → TaskFormComponent
│   /users      → UserListComponent
│
├── Services (singletons via providedIn: 'root')
│   TaskService  → HttpClient → .NET API /api/tasks
│   UserService  → HttpClient → .NET API /api/users
│
└── Components
    DashboardComponent  — forkJoin(tasks + users) → KPI stats
    TaskListComponent   — getAllTasks() → client-side filter
    TaskFormComponent   — create/edit mode from route :id param
    UserListComponent   — inline CRUD with reactive forms
```

**Key Angular patterns:**
- **Smart/Dumb components**: Smart components inject services; Dumb components receive `@Input()`.
- **Reactive Forms**: form structure defined in TypeScript (not HTML) for testability.
- **takeUntil pattern**: prevents Observable memory leaks when components are destroyed.
- **forkJoin**: parallel HTTP requests (same as `Promise.all`).
- **Environment files**: `environment.ts` (dev) ↔ `environment.prod.ts` (production) swapped at build time.
