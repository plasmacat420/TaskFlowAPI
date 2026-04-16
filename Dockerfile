# ─────────────────────────────────────────────────────────────────────────────
# Dockerfile — Multi-Stage Build for TaskFlow API
#
# Architectural Pattern: Multi-Stage Docker Build
#
# WHY multi-stage builds:
# Stage 1 (build): Uses the full SDK image (~300MB) to compile the application.
# Stage 2 (runtime): Uses the minimal runtime image (~80MB) for production.
# The final image contains ONLY the compiled output — no SDK, no source code, no build tools.
#
# WHY this matters:
# - Smaller image = faster deploys to Render, less attack surface (fewer packages to exploit)
# - Separation of build/runtime mirrors the principle of "minimal production footprint"
# - Build dependencies (compilers, dev tools) are never in the production image
#
# Build: docker build -t taskflow-api .
# Run:   docker run -p 8080:8080 -e DATABASE_URL="your_connection_string" taskflow-api
# ─────────────────────────────────────────────────────────────────────────────

# ── Stage 1: BUILD ────────────────────────────────────────────────────────────
# Use the official .NET 8 SDK image as the build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory inside the container for the build stage
WORKDIR /src

# Copy the project file first and restore dependencies
# WHY copy .csproj first: Docker caches each layer. If only source code changed
# (not the .csproj), the `dotnet restore` layer is reused from cache — much faster builds.
COPY TaskFlowAPI.csproj .
RUN dotnet restore

# Now copy all source files and build the application
COPY . .
RUN dotnet publish TaskFlowAPI.csproj -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── Stage 2: RUNTIME ──────────────────────────────────────────────────────────
# Use the minimal ASP.NET 8 runtime image (no SDK tooling)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Copy ONLY the published output from the build stage — no source code, no SDK
COPY --from=build /app/publish .

# Expose port 8080 — Render's default for web services
# ASP.NET Core listens on $PORT environment variable; we set it below
EXPOSE 8080

# Configure ASP.NET Core to listen on port 8080 (Render's expected port)
# Render sets $PORT=10000 but also proxies to 8080 — using 8080 is more portable
ENV ASPNETCORE_URLS=http://+:8080

# Set environment to Production — disables Swagger, enables production logging
ENV ASPNETCORE_ENVIRONMENT=Production

# Entrypoint: run the compiled .NET application
ENTRYPOINT ["dotnet", "TaskFlowAPI.dll"]
