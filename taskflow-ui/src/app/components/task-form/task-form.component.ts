/*
 * task-form.component.ts — Create/Edit Task Form Component
 *
 * Architectural Pattern: Smart Component + Reactive Forms
 *
 * WHY Reactive Forms over Template-Driven Forms:
 * Reactive Forms (FormGroup, FormControl, Validators) are:
 * 1. Type-safe — form structure is defined in TypeScript, not HTML
 * 2. Testable — you can test form validation logic in unit tests without a DOM
 * 3. Explicit — all validation rules are visible in the component class
 * 4. Powerful — easy to add cross-field validators, async validators, dynamic fields
 *
 * Template-driven forms use [(ngModel)] and are simpler for small forms
 * but harder to test and extend. Enterprise Angular typically uses Reactive Forms.
 *
 * This component handles both CREATE (/tasks/new) and EDIT (/tasks/edit/:id) modes.
 * The route parameter :id determines the mode. This "dual mode" pattern avoids
 * duplicating a nearly-identical form for two use cases.
 */

import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { TaskService } from '../../services/task.service';
import { UserService } from '../../services/user.service';
import { User } from '../../models/user.model';
import { TASK_STATUSES, TASK_PRIORITIES } from '../../models/task.model';

@Component({
  selector: 'app-task-form',
  templateUrl: './task-form.component.html',
  styleUrls: ['./task-form.component.css']
})
export class TaskFormComponent implements OnInit, OnDestroy {

  // ── State ──────────────────────────────────────────────────────────────────
  form!: FormGroup;             // Reactive form definition
  users: User[] = [];           // Available users for assignment dropdown
  loading = false;              // Loading state for initial data fetch (edit mode)
  submitting = false;           // Prevents double-submit on slow connections
  error: string | null = null;
  isEditMode = false;           // true if route has :id param
  taskId: string | null = null;

  readonly statuses = TASK_STATUSES;
  readonly priorities = TASK_PRIORITIES;

  private destroy$ = new Subject<void>();

  constructor(
    /** FormBuilder — DI-provided service for building reactive form groups */
    private fb: FormBuilder,
    private taskService: TaskService,
    private userService: UserService,
    /** ActivatedRoute — provides access to the current route's params (:id) */
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.detectMode();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Form Setup ─────────────────────────────────────────────────────────────

  /**
   * Builds the reactive form with initial values and validators.
   * FormBuilder.group() creates a FormGroup — a collection of FormControls.
   * Each FormControl can have synchronous validators (Validators.*) and async validators.
   *
   * WHY declare form shape here (not in HTML):
   * The form's structure, defaults, and rules are all in one place in the TypeScript class.
   * The HTML template only binds to [formControlName] — it doesn't define the rules.
   * This makes validation logic unit-testable without rendering the component.
   */
  private buildForm(): void {
    this.form = this.fb.group({
      title:          ['', [Validators.required, Validators.maxLength(200)]],
      description:    ['', [Validators.maxLength(2000)]],
      status:         ['Pending'],
      priority:       ['Medium'],
      dueDate:        [null],
      assignedUserId: [null]
    });
  }

  /**
   * Detects if we're in create or edit mode based on the route parameter.
   * Edit mode: /tasks/edit/:id — loads existing task data and pre-fills the form.
   * Create mode: /tasks/new — form starts blank.
   */
  private detectMode(): void {
    this.taskId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.taskId;

    if (this.isEditMode) {
      this.loading = true;
      // Load task AND users in parallel with forkJoin
      forkJoin({
        task: this.taskService.getTaskById(this.taskId!),
        users: this.userService.getAllUsers()
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ task, users }) => {
          this.users = users;
          // Patch form values with the loaded task data
          // patchValue: updates only the fields you provide (safe for partial updates)
          this.form.patchValue({
            title: task.title,
            description: task.description ?? '',
            status: task.status,
            priority: task.priority,
            // Format datetime for HTML date input (YYYY-MM-DD)
            dueDate: task.dueDate ? task.dueDate.substring(0, 10) : null,
            assignedUserId: task.assignedUserId
          });
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load task data.';
          this.loading = false;
        }
      });
    } else {
      // Create mode: just load users for the assignment dropdown
      this.userService.getAllUsers()
        .pipe(takeUntil(this.destroy$))
        .subscribe(users => this.users = users);
    }
  }

  // ── Submission ─────────────────────────────────────────────────────────────

  /**
   * Handles form submission.
   * Guards against invalid form and double-submits.
   * Dispatches to create or update based on isEditMode.
   */
  onSubmit(): void {
    // form.markAllAsTouched() — triggers validation display for all fields
    // (without this, validation errors only show after the user touches each field)
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.submitting = true;
    this.error = null;

    const formValue = this.form.value;
    const payload = {
      ...formValue,
      // Send null for empty assignedUserId (not empty string '')
      assignedUserId: formValue.assignedUserId || null,
      dueDate: formValue.dueDate || null
    };

    const request$ = this.isEditMode
      ? this.taskService.updateTask(this.taskId!, payload)
      : this.taskService.createTask(payload);

    request$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          // Navigate back to task list on success
          this.router.navigate(['/tasks']);
        },
        error: (err) => {
          this.error = err.status === 0
            ? 'Cannot reach the API — it may still be waking up. Wait ~30 seconds and try again.'
            : `Failed to ${this.isEditMode ? 'update' : 'create'} task. Please try again.`;
          this.submitting = false;
          console.error(err);
        }
      });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  cancel(): void {
    this.router.navigate(['/tasks']);
  }

  /** Returns true if a form control is invalid and has been touched */
  isFieldInvalid(fieldName: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control?.invalid && control?.touched);
  }

  /** Returns the first validation error message for a field */
  getFieldError(fieldName: string): string {
    const control = this.form.get(fieldName);
    if (!control?.errors) return '';
    if (control.errors['required']) return `${fieldName} is required.`;
    if (control.errors['maxlength']) return `Too long (max ${control.errors['maxlength'].requiredLength} chars).`;
    return 'Invalid value.';
  }
}
