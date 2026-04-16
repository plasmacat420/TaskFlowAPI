# TaskFlow UI

Angular frontend for the TaskFlow task management application.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | Angular 17 (NgModule-based) |
| Language | TypeScript 5.4 (strict mode) |
| HTTP | Angular HttpClient + RxJS Observables |
| Forms | Angular Reactive Forms |
| Routing | Angular Router |
| Styling | Pure CSS (no external UI library) |
| Hosting | GitHub Pages |

## Features

- **Dashboard** — KPI summary cards (total, pending, in-progress, done), progress bar, team overview
- **Task List** — client-side filtering by status, priority, and search text; delete with confirmation
- **Task Form** — reactive form for creating and editing tasks; validates all fields; user assignment dropdown
- **User List** — inline CRUD for team members; task count per user

## Component Architecture

```
AppComponent (shell — nav bar + <router-outlet>)
├── DashboardComponent   /dashboard   — KPIs, recent tasks, team summary
├── TaskListComponent    /tasks       — filtered task grid
├── TaskFormComponent    /tasks/new   — create task
│                        /tasks/edit/:id — edit task
└── UserListComponent    /users       — user management table
```

## Services

- `TaskService` — wraps all `/api/tasks` HTTP calls
- `UserService` — wraps all `/api/users` HTTP calls

Both services use `providedIn: 'root'` (singleton, app-wide injection).

## Local Development

### Prerequisites
- Node.js 18+
- Angular CLI: `npm install -g @angular/cli`

### Setup

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Start the development server:**
   ```bash
   npm start
   # or: ng serve
   ```
   App runs at `http://localhost:4200`. Points to `http://localhost:5000/api` (local .NET backend).

3. **Make sure the TaskFlow API is running:**
   ```bash
   cd ../TaskFlowAPI
   dotnet run
   ```

## Deploy to GitHub Pages

1. Update `REPO_NAME` in `deploy.sh` to match your GitHub repository name.
2. Update `environment.prod.ts` with your actual Render backend URL.
3. Run the deploy script:
   ```bash
   chmod +x deploy.sh
   ./deploy.sh
   ```
4. Enable GitHub Pages in your repo settings → Pages → Source: `gh-pages` branch.

Your app will be live at: `https://yourusername.github.io/your-repo-name/`

## Key Angular Concepts Demonstrated

| Concept | Where |
|---------|-------|
| NgModule / DI | `app.module.ts` |
| Router + routerLink | `app-routing.module.ts`, templates |
| Smart/Dumb Components | All components |
| Reactive Forms + Validators | `task-form.component.ts`, `user-list.component.ts` |
| HttpClient + Observables | `task.service.ts`, `user.service.ts` |
| forkJoin (parallel HTTP) | `dashboard.component.ts`, `task-form.component.ts` |
| takeUntil memory leak prevention | All components |
| Environment-based config | `environments/` |
| CSS custom properties | `styles.css` |
| Responsive layout | All component CSS |
