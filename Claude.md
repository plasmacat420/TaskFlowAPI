Build TaskFlow — a full-stack Task Management app
Build a production-ready full-stack Task Management application called TaskFlow designed to demonstrate .NET, C#, REST API, Entity Framework, PostgreSQL, Microservices-ready architecture, and Angular skills for a job interview showcase.

Backend — .NET Core Web API
Build a clean layered REST API with the following layers:

Models — Task and User entities
DbContext — PostgreSQL via Entity Framework Core
Repository layer — data access abstraction
Service layer — business logic
Controllers — REST endpoints

Entities needed:

TaskItem — Id, Title, Description, Status (Pending/InProgress/Done), Priority (Low/Medium/High), DueDate, CreatedAt, AssignedUserId
User — Id, Name, Email, CreatedAt

Endpoints needed:

CRUD for Tasks
CRUD for Users
Get tasks by user
Get tasks by status
Get tasks by priority

Requirements:

async/await throughout
Dependency Injection for all services and repositories
LINQ queries in repository layer
Proper HTTP status codes (200, 201, 204, 404)
CORS enabled for GitHub Pages frontend URL
Swagger/OpenAPI documentation enabled
Environment variable for connection string (for Render deployment)
Well commented code explaining WHY each pattern is used, not just what — comments should teach the reader about DI, async, LINQ, EF, layered architecture as they read

Deploy backend to Render as a web service:

Include a Dockerfile for containerized deployment
Include render.yaml for Render configuration
README with setup instructions and architecture explanation


Frontend — Angular
Build a clean Angular frontend called TaskFlow UI:

Dashboard showing task summary cards (total, pending, in progress, done)
Task list with filter by status and priority
Create/Edit task form
Assign task to user
Delete task with confirmation
Responsive design using just CSS (no external UI library)
Angular services for API calls
Proper component structure — at least 4 components
Environment file pointing to Render backend URL

Deploy frontend to GitHub Pages:

Include angular.json configured for GitHub Pages base href
Include deploy.sh script using npx angular-cli-ghpages
README with deployment steps


Documentation requirements
Every file must have a header comment explaining:

what this file does
which architectural pattern it represents
why this pattern is used in enterprise .NET applications

Every non-trivial method must have an XML doc comment (///) explaining what it does and why.
Include a root-level ARCHITECTURE.md explaining:

the full layered architecture diagram in ASCII
why each layer exists
how data flows from HTTP request to database and back
how this maps to microservices concepts


Folder structure expected:
TaskFlowAPI/          ← .NET backend
  Models/
  Data/
  Repositories/
  Services/
  Controllers/
  Dockerfile
  render.yaml
  README.md

taskflow-ui/          ← Angular frontend
  src/app/
    components/
    services/
    models/
  deploy.sh
  README.md

ARCHITECTURE.md       ← root level

Goal: when complete, the developer should be able to explain every file, every pattern, and every decision confidently in a technical interview. The code comments are the learning material.