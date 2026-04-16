/*
 * task.service.ts — Angular Service for Task API Communication
 *
 * Architectural Pattern: Angular Service (Singleton via providedIn: 'root')
 *
 * WHY Angular Services:
 * Services encapsulate cross-cutting concerns — in this case, HTTP communication.
 * By putting all API calls in a service (not in components), we achieve:
 * 1. Reusability — any component can inject TaskService without duplicating HTTP logic
 * 2. Testability — mock the service in component unit tests (no real HTTP calls)
 * 3. Single source of truth — the API URL and request format are defined once
 * 4. Separation of concerns — components handle UI, services handle data
 *
 * WHY RxJS Observables instead of Promises:
 * Angular's HttpClient returns Observables (from RxJS).
 * Observables are more powerful than Promises:
 * - Can be cancelled (unsubscribe to abort an HTTP request)
 * - Can be composed (pipe, map, catchError)
 * - Integrate with Angular's async pipe in templates (auto-subscribe/unsubscribe)
 * - Support streaming (WebSockets, Server-Sent Events use the same Observable pattern)
 *
 * providedIn: 'root' makes this service a singleton — one instance shared app-wide.
 * This is the same as a Singleton service registration in .NET DI.
 */

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Task, CreateTaskRequest, UpdateTaskRequest, TaskStatus, TaskPriority } from '../models/task.model';

@Injectable({
  // providedIn: 'root' — Angular registers this service in the root injector.
  // It's a singleton: the same instance is shared across all components.
  // Equivalent to builder.Services.AddSingleton<TaskService>() in .NET.
  providedIn: 'root'
})
export class TaskService {

  /** Base API URL — read from the environment file (swapped at build time) */
  private readonly apiUrl = `${environment.apiUrl}/tasks`;

  /**
   * HttpClient is Angular's built-in HTTP client — injected via DI.
   * It wraps the browser's fetch API with Observable support and interceptor hooks.
   * WHY inject HttpClient instead of using fetch directly: testable, interceptable, typed.
   */
  constructor(private http: HttpClient) {}

  /**
   * Fetches all tasks from GET /api/tasks.
   * Returns an Observable — components subscribe to get the data.
   * The generic type parameter <Task[]> tells TypeScript what shape to expect from the API.
   */
  getAllTasks(): Observable<Task[]> {
    return this.http.get<Task[]>(this.apiUrl);
  }

  /**
   * Fetches a single task by ID from GET /api/tasks/{id}.
   */
  getTaskById(id: string): Observable<Task> {
    return this.http.get<Task>(`${this.apiUrl}/${id}`);
  }

  /**
   * Fetches tasks assigned to a specific user from GET /api/tasks/user/{userId}.
   */
  getTasksByUser(userId: string): Observable<Task[]> {
    return this.http.get<Task[]>(`${this.apiUrl}/user/${userId}`);
  }

  /**
   * Fetches tasks by workflow status from GET /api/tasks/status/{status}.
   */
  getTasksByStatus(status: TaskStatus): Observable<Task[]> {
    return this.http.get<Task[]>(`${this.apiUrl}/status/${status}`);
  }

  /**
   * Fetches tasks by priority from GET /api/tasks/priority/{priority}.
   */
  getTasksByPriority(priority: TaskPriority): Observable<Task[]> {
    return this.http.get<Task[]>(`${this.apiUrl}/priority/${priority}`);
  }

  /**
   * Creates a new task via POST /api/tasks.
   * Returns the created Task (with server-generated ID and timestamps).
   */
  createTask(task: CreateTaskRequest): Observable<Task> {
    return this.http.post<Task>(this.apiUrl, task);
  }

  /**
   * Updates an existing task via PUT /api/tasks/{id}.
   */
  updateTask(id: string, task: UpdateTaskRequest): Observable<Task> {
    return this.http.put<Task>(`${this.apiUrl}/${id}`, task);
  }

  /**
   * Deletes a task via DELETE /api/tasks/{id}.
   * Returns void — no body on 204 No Content response.
   */
  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
