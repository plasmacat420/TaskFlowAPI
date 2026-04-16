/*
 * app-routing.module.ts — Angular Router Configuration
 *
 * Architectural Pattern: Front Controller / Router (SPA Navigation)
 *
 * WHY Angular Router:
 * In a Single Page Application (SPA), navigation happens client-side —
 * the browser never fully reloads. The Angular Router intercepts URL changes,
 * matches them to route definitions, and renders the corresponding component.
 *
 * This is analogous to ASP.NET Core's route configuration — same concept, client-side.
 *
 * WHY lazy loading (loadComponent / loadChildren):
 * For large apps, lazy loading only downloads a component's code when the user navigates to it.
 * For this showcase app, we use direct component imports (simpler, appropriate for app size).
 */

import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { TaskListComponent } from './components/task-list/task-list.component';
import { TaskFormComponent } from './components/task-form/task-form.component';
import { UserListComponent } from './components/user-list/user-list.component';

/**
 * Route definitions — maps URL paths to Angular components.
 * The router renders the matched component inside <router-outlet> in app.component.html.
 */
const routes: Routes = [
  // Default route — redirect empty path to the dashboard
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },

  // Dashboard — task summary KPIs
  { path: 'dashboard', component: DashboardComponent },

  // Task list — filterable list of all tasks
  { path: 'tasks', component: TaskListComponent },

  // Create task — blank form
  { path: 'tasks/new', component: TaskFormComponent },

  // Edit task — form pre-populated with task data (:id is a route parameter)
  { path: 'tasks/edit/:id', component: TaskFormComponent },

  // User management — list and CRUD for users
  { path: 'users', component: UserListComponent },

  // Wildcard route — redirects unknown URLs to dashboard
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
