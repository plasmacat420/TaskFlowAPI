/*
 * app.module.ts — Root Angular Module (NgModule)
 *
 * Architectural Pattern: Module (NgModule / Feature Module pattern)
 *
 * WHY NgModule:
 * Angular's NgModule system is the DI container + component registry.
 * It declares what components/directives/pipes exist, what Angular modules to import,
 * and what services/components to export for use in other modules.
 *
 * AppModule is the root module — it bootstraps the application.
 * In a large enterprise app, you'd have feature modules (TaskModule, UserModule),
 * shared modules (SharedModule), and lazy-loaded modules to split bundle sizes.
 *
 * For this showcase, a single AppModule is appropriate and clear.
 */

import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { ReactiveFormsModule } from '@angular/forms';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { TaskListComponent } from './components/task-list/task-list.component';
import { TaskFormComponent } from './components/task-form/task-form.component';
import { UserListComponent } from './components/user-list/user-list.component';

@NgModule({
  declarations: [
    // All components, directives, and pipes that belong to this module
    AppComponent,
    DashboardComponent,
    TaskListComponent,
    TaskFormComponent,
    UserListComponent
  ],
  imports: [
    // BrowserModule — provides browser-specific services (DOM rendering, sanitization)
    // Must be imported once in the root module only
    BrowserModule,

    // AppRoutingModule — our route configuration (see app-routing.module.ts)
    AppRoutingModule,

    // HttpClientModule — enables Angular's HttpClient for all services
    // Provides the HTTP DI token used by TaskService and UserService
    HttpClientModule,

    // ReactiveFormsModule — enables FormGroup, FormControl, Validators
    // Reactive forms are type-safe and testable (vs template-driven forms)
    ReactiveFormsModule,

    // FormsModule — enables [(ngModel)] two-way binding (used for filter dropdowns)
    FormsModule
  ],
  // bootstrap: the root component Angular renders into <app-root> in index.html
  bootstrap: [AppComponent]
})
export class AppModule { }
