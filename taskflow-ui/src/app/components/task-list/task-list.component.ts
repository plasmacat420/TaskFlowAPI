/*
 * task-list.component.ts — Task List with Filtering
 *
 * Architectural Pattern: Smart/Container Component
 *
 * Responsibilities:
 * - Load all tasks from the API
 * - Allow filtering by status and priority (client-side filtering — no extra API calls)
 * - Delegate delete to the service, confirm via browser dialog
 * - Navigate to edit form on edit button click
 *
 * Client-side filtering pattern:
 * We load all tasks once and filter locally in memory.
 * WHY: Avoids extra API calls on every filter change. Acceptable for small datasets.
 * For large datasets (10k+ tasks), server-side filtering/pagination would be needed.
 */

import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { TaskService } from '../../services/task.service';
import { Task, TaskStatus, TaskPriority, TASK_STATUSES, TASK_PRIORITIES } from '../../models/task.model';

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.css']
})
export class TaskListComponent implements OnInit, OnDestroy {

  // ── State ──────────────────────────────────────────────────────────────────
  allTasks: Task[] = [];          // full list from API
  filteredTasks: Task[] = [];     // displayed after filter applied
  loading = true;
  error: string | null = null;
  successMessage: string | null = null;

  // ── Filter state ───────────────────────────────────────────────────────────
  filterStatus: string = '';      // '' means "all"
  filterPriority: string = '';    // '' means "all"
  searchText: string = '';

  // ── Dropdown options ───────────────────────────────────────────────────────
  readonly statuses = TASK_STATUSES;
  readonly priorities = TASK_PRIORITIES;

  private destroy$ = new Subject<void>();

  constructor(
    private taskService: TaskService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadTasks();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Data Loading ───────────────────────────────────────────────────────────

  loadTasks(): void {
    this.loading = true;
    this.taskService.getAllTasks()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tasks) => {
          this.allTasks = tasks;
          this.applyFilters();
          this.loading = false;
        },
        error: (err) => {
          this.error = 'Failed to load tasks. Is the API running?';
          this.loading = false;
          console.error(err);
        }
      });
  }

  // ── Filtering ──────────────────────────────────────────────────────────────

  /**
   * Applies all active filters to allTasks and updates filteredTasks.
   * Called whenever any filter input changes (via (ngModelChange) in the template).
   *
   * WHY compute in JS not via more API calls:
   * Once the tasks are loaded, filtering is a pure in-memory operation (O(n)).
   * No round-trip needed — instant feedback for the user.
   */
  applyFilters(): void {
    this.filteredTasks = this.allTasks.filter(task => {
      const matchesStatus = !this.filterStatus || task.status === this.filterStatus;
      const matchesPriority = !this.filterPriority || task.priority === this.filterPriority;
      const matchesSearch = !this.searchText ||
        task.title.toLowerCase().includes(this.searchText.toLowerCase()) ||
        (task.description?.toLowerCase().includes(this.searchText.toLowerCase()) ?? false);
      return matchesStatus && matchesPriority && matchesSearch;
    });
  }

  clearFilters(): void {
    this.filterStatus = '';
    this.filterPriority = '';
    this.searchText = '';
    this.applyFilters();
  }

  // ── Actions ────────────────────────────────────────────────────────────────

  editTask(task: Task): void {
    // Navigate to the edit form with the task ID as a route parameter
    this.router.navigate(['/tasks/edit', task.id]);
  }

  deleteTask(task: Task): void {
    // Browser confirm dialog — simple UX for a showcase app
    // Production: use a custom modal component for better UX and testability
    if (!confirm(`Delete task "${task.title}"? This cannot be undone.`)) return;

    this.taskService.deleteTask(task.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          // Remove from local array — no need to re-fetch from API
          this.allTasks = this.allTasks.filter(t => t.id !== task.id);
          this.applyFilters();
          this.successMessage = `Task "${task.title}" deleted.`;
          setTimeout(() => this.successMessage = null, 3000);
        },
        error: () => {
          this.error = 'Failed to delete task. Please try again.';
        }
      });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      'Pending': 'badge-pending',
      'InProgress': 'badge-inprogress',
      'Done': 'badge-done'
    };
    return map[status] ?? '';
  }

  getPriorityClass(priority: string): string {
    const map: Record<string, string> = {
      'Low': 'badge-low',
      'Medium': 'badge-medium',
      'High': 'badge-high'
    };
    return map[priority] ?? '';
  }

  formatDate(dateStr: string | null): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString();
  }

  isOverdue(task: Task): boolean {
    if (!task.dueDate || task.status === 'Done') return false;
    return new Date(task.dueDate) < new Date();
  }
}
