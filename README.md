# TaskFlow API

A production-ready REST API for task management, built with **.NET 8**, **Entity Framework Core**, and **PostgreSQL**. Designed to demonstrate enterprise .NET patterns for technical interviews.

## Architecture

```
HTTP Request
     │
     ▼
┌─────────────┐
│  Controller │  ← Maps HTTP to/from service calls. HTTP-only concerns.
└──────┬──────┘
       │ ITaskService / IUserService
       ▼
┌─────────────┐
│   Service   │  ← Business logic, validation, DTO↔Entity mapping.
└──────┬──────┘
       │ ITaskRepository / IUserRepository
       ▼
┌─────────────┐
│ Repository  │  ← All EF Core / SQL. The only layer that knows about the DB.
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  PostgreSQL │  ← via Entity Framework Core + Npgsql provider
└─────────────┘
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | .NET 8 Web API |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL (Npgsql provider) |
| API Docs | Swagger / OpenAPI (Swashbuckle) |
| Containerization | Docker (multi-stage build) |
| Hosting | Render |

## API Endpoints

### Tasks
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tasks` | Get all tasks |
| GET | `/api/tasks/{id}` | Get task by ID |
| GET | `/api/tasks/user/{userId}` | Get tasks by assigned user |
| GET | `/api/tasks/status/{status}` | Get tasks by status (Pending/InProgress/Done) |
| GET | `/api/tasks/priority/{priority}` | Get tasks by priority (Low/Medium/High) |
| POST | `/api/tasks` | Create new task |
| PUT | `/api/tasks/{id}` | Update task |
| DELETE | `/api/tasks/{id}` | Delete task |

### Users
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | Get all users |
| GET | `/api/users/{id}` | Get user by ID |
| POST | `/api/users` | Create new user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Delete user |

## Local Development

### Prerequisites
- .NET 8 SDK
- PostgreSQL 15+
- Docker (optional)

### Setup

1. **Clone the repo and navigate to the API folder:**
   ```bash
   cd TaskFlowAPI
   ```

2. **Update the connection string** in `appsettings.Development.json`:
   ```json
   "DefaultConnection": "Host=localhost;Database=taskflow;Username=postgres;Password=yourpassword"
   ```

3. **Apply database migrations:**
   ```bash
   dotnet ef database update
   ```
   > First time? Create the migration: `dotnet ef migrations add InitialCreate`

4. **Run the API:**
   ```bash
   dotnet run
   ```
   API starts at `http://localhost:5000`. Swagger UI at `http://localhost:5000`.

### Docker (local)
```bash
docker build -t taskflow-api .
docker run -p 8080:8080 \
  -e DATABASE_URL="Host=host.docker.internal;Database=taskflow;Username=postgres;Password=yourpassword" \
  taskflow-api
```

## Deploy to Render

1. Push this repo to GitHub.
2. Go to [render.com](https://render.com) → **New** → **Blueprint**.
3. Connect your GitHub repository.
4. Render auto-detects `render.yaml` and provisions:
   - A PostgreSQL database
   - The web service (Docker-based)
5. EF Core migrations run automatically on startup via `db.Database.Migrate()` in `Program.cs`.

## Design Patterns Used

| Pattern | Where | Why |
|---------|-------|-----|
| Repository | `Repositories/` | Isolates data access; enables testing with fakes |
| Service Layer | `Services/` | Business logic separate from HTTP and DB concerns |
| DTO | `DTOs/` | Decouples API contract from DB schema; prevents over-posting |
| Dependency Injection | `Program.cs` | Loose coupling, testability, lifetime management |
| Unit of Work | `AppDbContext` | All changes in one request commit/rollback together |

## Key .NET Concepts Demonstrated

- **async/await** — all I/O operations are non-blocking
- **LINQ** — type-safe queries translated to SQL by EF Core
- **Nullable reference types** — compile-time null safety (`string?`, `Guid?`)
- **Records / DTOs** — explicit API contracts
- **[ApiController]** — automatic 400 responses for model validation failures
- **Enum to int DB mapping** — storage efficiency with readable API values
- **Multi-stage Dockerfile** — small production images
