/*
 * dashboard.component.ts — Dashboard Summary Component
 *
 * Architectural Pattern: Smart/Container Component
 *
 * WHY "Smart" Component:
 * Angular components are typically divided into:
 * - Smart (Container): fetches data, holds state, passes to Presentational children
 * - Dumb (Presentational): receives @Input(), emits @Output(), no service dependencies
 *
 * Dashboard is a Smart Component — it injects services, calls the API, and owns the data.
 * In a larger app, each stat card would be a Dumb Component receiving data via @Input().
 *
 * WHY OnInit:
 * Data fetching goes in ngOnInit (not the constructor).
 * Constructor should only do DI (inject services). ngOnInit runs after Angular
 * initializes the component's inputs — safe to fetch data here.
 */

import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { TaskService } from '../../services/task.service';
import { UserService } from '../../services/user.service';
import { Task } from '../../models/task.model';
import { User } from '../../models/user.model';

/**
 * Dashboard component — shows KPI summary cards and recent activity.
 * Route: /dashboard
 */
@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {

  // ── State ──────────────────────────────────────────────────────────────────

  tasks: Task[] = [];
  users: User[] = [];
  loading = true;
  error: string | null = null;

  /**
   * Subject used for clean Observable unsubscription.
   * WHY: Subscriptions that aren't cleaned up cause memory leaks in SPAs
   * because the component is destroyed but the Observable keeps emitting.
   * takeUntil(this.destroy$) automatically unsubscribes when destroy$ emits.
   */
  private destroy$ = new Subject<void>();

  constructor(
    private taskService: TaskService,
    private userService: UserService
  ) {}

  // ── Computed KPI getters ───────────────────────────────────────────────────

  /** Total number of tasks */
  get totalTasks(): number { return this.tasks.length; }

  /** Tasks currently in Pending state */
  get pendingTasks(): number {
    return this.tasks.filter(t => t.status === 'Pending').length;
  }

  /** Tasks currently in progress */
  get inProgressTasks(): number {
    return this.tasks.filter(t => t.status === 'InProgress').length;
  }

  /** Completed tasks */
  get doneTasks(): number {
    return this.tasks.filter(t => t.status === 'Done').length;
  }

  /** High-priority tasks that are still open */
  get highPriorityOpen(): number {
    return this.tasks.filter(t => t.priority === 'High' && t.status !== 'Done').length;
  }

  /** Completion percentage */
  get completionRate(): number {
    if (this.totalTasks === 0) return 0;
    return Math.round((this.doneTasks / this.totalTasks) * 100);
  }

  /** Most recent 5 tasks for the "recent activity" list */
  get recentTasks(): Task[] {
    return [...this.tasks]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5);
  }

  /** Today's date for the dashboard header */
  today = new Date();

  /** Returns CSS badge class for a task status */
  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      'Pending': 'badge-pending',
      'InProgress': 'badge-inprogress',
      'Done': 'badge-done'
    };
    return map[status] ?? '';
  }

  /** Returns CSS badge class for a priority level */
  getPriorityClass(priority: string): string {
    const map: Record<string, string> = {
      'Low': 'badge-low',
      'Medium': 'badge-medium',
      'High': 'badge-high'
    };
    return map[priority] ?? '';
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit(): void {
    /**
     * forkJoin — runs multiple Observables in parallel and emits when ALL complete.
     * Equivalent to Promise.all() — efficient when both requests are independent.
     * WHY not sequential: loading tasks and users in parallel halves the wait time.
     */
    forkJoin({
      tasks: this.taskService.getAllTasks(),
      users: this.userService.getAllUsers()
    })
    .pipe(takeUntil(this.destroy$))  // auto-unsubscribe when component is destroyed
    .subscribe({
      next: ({ tasks, users }) => {
        this.tasks = tasks;
        this.users = users;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load dashboard data. Is the API running?';
        this.loading = false;
        console.error('Dashboard load error:', err);
      }
    });
  }

  ngOnDestroy(): void {
    // Emit a value to trigger takeUntil, then complete the subject
    this.destroy$.next();
    this.destroy$.complete();
  }
}
