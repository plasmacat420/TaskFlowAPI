/*
 * Program.cs — Application Entry Point & Dependency Injection Composition Root
 *
 * Architectural Pattern: Composition Root + Middleware Pipeline
 *
 * WHY this file is critical:
 * Program.cs is the "Composition Root" — the single place in the application where
 * the entire dependency graph is assembled. Every interface → implementation mapping
 * is registered here. ASP.NET Core's built-in DI container reads these registrations
 * and injects the right dependencies when constructing controllers and services.
 *
 * The file has two distinct sections:
 * 1. SERVICE REGISTRATION (builder phase) — "what classes exist and how they relate"
 * 2. MIDDLEWARE PIPELINE (app phase)      — "in what order should HTTP requests be processed"
 *
 * WHY DI (Dependency Injection):
 * Without DI, every class would `new` its own dependencies:
 *   var controller = new TasksController(new TaskService(new TaskRepository(new AppDbContext(...))));
 * This creates tight coupling (hard to test, hard to change) and uncontrolled object lifetimes.
 * With DI, the framework manages object creation, lifetime, and disposal automatically.
 *
 * Lifetime scopes explained:
 * - Singleton  — one instance for the entire application lifetime (e.g., config, logging)
 * - Scoped     — one instance per HTTP request (e.g., DbContext, repositories, services)
 * - Transient  — new instance every time it's requested (e.g., lightweight, stateless utilities)
 */

using Microsoft.EntityFrameworkCore;
using TaskFlowAPI.Data;
using TaskFlowAPI.Repositories;
using TaskFlowAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 1. CONTROLLERS & API DOCUMENTATION ───────────────────────────────────────

builder.Services.AddControllers()
    // Configure JSON serialization: serialize enum values as their string names ("InProgress")
    // not as integers (1). This makes the API self-documenting and easier to consume.
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// AddEndpointsApiExplorer + AddSwaggerGen: wire up Swagger/OpenAPI
// Swagger reads [ProducesResponseType] attributes and XML comments to generate API docs
// Available at /swagger when running in Development
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "TaskFlow API",
        Version = "v1",
        Description = "A production-ready Task Management REST API built with .NET 8, " +
                      "Entity Framework Core, and PostgreSQL. Demonstrates layered architecture, " +
                      "Repository pattern, Dependency Injection, and async/await throughout."
    });
});

// ── 2. DATABASE (Entity Framework Core + PostgreSQL) ─────────────────────────

// Register AppDbContext with PostgreSQL provider
// Connection string loaded from environment variable DATABASE_URL (Render) or
// from appsettings.json (local development)
// WHY environment variable: secrets must NEVER be hardcoded in source code.
// Render injects DATABASE_URL automatically for PostgreSQL services.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    // render.yaml sets env var "ConnectionStrings__DefaultConnection" (double underscore).
    // ASP.NET Core's configuration system maps this directly to ConnectionStrings:DefaultConnection,
    // so GetConnectionString() returns the full value injected by Render — no manual env var parsing.
    // Locally, appsettings.Development.json provides the value.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException(
            "Database connection string not found. " +
            "Set 'ConnectionStrings:DefaultConnection' in appsettings.Development.json for local dev, " +
            "or ensure the Render service has ConnectionStrings__DefaultConnection set.");

    // Render provides the connection string as a PostgreSQL URI: postgres://user:pass@host:port/db
    // Npgsql supports URI format natively.
    // Append SSL settings required by Render's managed PostgreSQL.
    if (!connectionString.Contains("SSL Mode") && !connectionString.Contains("sslmode"))
    {
        var sep = connectionString.Contains('?') ? "&" : "?";
        connectionString += $"{sep}sslmode=require&Trust Server Certificate=true";
    }

    options.UseNpgsql(connectionString, npgsql =>
        // Retry on transient failures — guards against the DB container not being ready
        // on first cold start (common on free-tier Render where services spin down)
        npgsql.EnableRetryOnFailure(maxRetryCount: 3));
});

// ── 3. REPOSITORIES (Data Access Layer) ──────────────────────────────────────

// Register repositories as Scoped (one instance per HTTP request)
// WHY Scoped for repositories: they depend on DbContext which is also Scoped.
// A Scoped dependency cannot consume a Singleton dependency safely (captive dependency problem).
// DI container enforces this — registering a Scoped service as Singleton would throw at startup.
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// ── 4. SERVICES (Business Logic Layer) ───────────────────────────────────────

builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IUserService, UserService>();

// ── 5. CORS (Cross-Origin Resource Sharing) ──────────────────────────────────

// CORS allows the browser to make requests from one origin (GitHub Pages frontend)
// to a different origin (Render backend). Without CORS, browsers block these requests.
// This is a browser security feature — it doesn't apply to server-to-server calls.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            // Allow GitHub Pages deployment URL — change "yourusername" to your GitHub username
            // Also allows localhost for local development
            .WithOrigins(
                "https://plasmacat420.github.io",   // GitHub Pages
                "http://localhost:4200",             // Angular dev server
                "http://localhost:3000"              // Alternative dev server
            )
            .AllowAnyHeader()   // Allow Content-Type, Authorization, etc.
            .AllowAnyMethod();  // Allow GET, POST, PUT, DELETE, OPTIONS (preflight)
    });
});

var app = builder.Build();

// ── 6. MIDDLEWARE PIPELINE ────────────────────────────────────────────────────

// Middleware processes every HTTP request in the order it's registered here.
// Think of it as an "onion" — request goes through each layer, response comes back through each.

// Swagger UI — only in Development to avoid exposing API docs in production
// (In production on Render, this is not Development, so Swagger is disabled unless you change this)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
        c.RoutePrefix = string.Empty;  // Swagger at root URL (/) for convenience
    });
}

// Redirect HTTP to HTTPS — security baseline
// Render handles TLS termination, so this is also handled at the proxy level
app.UseHttpsRedirection();

// Apply CORS policy BEFORE routing/authorization
// WHY before: CORS preflight (OPTIONS) requests need to be answered before auth middleware runs
// Otherwise the preflight returns 401 and the browser blocks the actual request
app.UseCors("AllowFrontend");

// Route requests to the correct controller action
app.UseRouting();

// Authorization middleware — placeholder for future JWT/OAuth integration
// Currently no auth is configured but the middleware is wired in the correct position
app.UseAuthorization();

// Map controller routes — connects [Route] attributes to controller actions
app.MapControllers();

// ── 7. AUTO-MIGRATE ON STARTUP ────────────────────────────────────────────────

// In production (Render), apply pending EF Core migrations automatically on startup.
// WHY: Render doesn't give you a manual step to run `dotnet ef database update`.
// This ensures the DB schema is always in sync with the code on every deploy.
// In a large team, you'd use a dedicated migration job instead to avoid race conditions
// when multiple instances start simultaneously.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();  // applies all pending migrations; no-op if up to date
}

app.Run();
