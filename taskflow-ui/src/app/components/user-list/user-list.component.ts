/*
 * user-list.component.ts — User Management Component
 *
 * Architectural Pattern: Smart Component with inline edit pattern
 *
 * This component handles the full CRUD lifecycle for users:
 * - List all users with their task counts
 * - Inline create form at the top
 * - Inline edit on each row
 * - Delete with confirmation
 *
 * WHY inline CRUD (not separate pages for users):
 * Users have a simpler data model (name + email only).
 * A separate page for each CRUD operation would be over-engineering for this use case.
 * The Angular Router is used for tasks (more complex form) but not for this simpler use case.
 */

import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { UserService } from '../../services/user.service';
import { User } from '../../models/user.model';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.css']
})
export class UserListComponent implements OnInit, OnDestroy {

  users: User[] = [];
  loading = true;
  error: string | null = null;
  successMessage: string | null = null;

  // Create form
  createForm!: FormGroup;
  showCreateForm = false;
  creating = false;

  // Edit state
  editingUserId: string | null = null;
  editForm!: FormGroup;
  saving = false;

  private destroy$ = new Subject<void>();

  constructor(
    private userService: UserService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.buildForms();
    this.loadUsers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Forms ──────────────────────────────────────────────────────────────────

  private buildForms(): void {
    // Create form — starts empty
    this.createForm = this.fb.group({
      name:  ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]]
    });

    // Edit form — populated when user clicks Edit
    this.editForm = this.fb.group({
      name:  ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]]
    });
  }

  // ── Data ───────────────────────────────────────────────────────────────────

  loadUsers(): void {
    this.userService.getAllUsers()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (users) => {
          this.users = users;
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load users. Is the API running?';
          this.loading = false;
        }
      });
  }

  // ── Create ─────────────────────────────────────────────────────────────────

  toggleCreateForm(): void {
    this.showCreateForm = !this.showCreateForm;
    if (!this.showCreateForm) this.createForm.reset();
  }

  onCreate(): void {
    this.createForm.markAllAsTouched();
    if (this.createForm.invalid) return;

    this.creating = true;
    this.error = null;

    this.userService.createUser(this.createForm.value)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (user) => {
          this.users = [...this.users, user].sort((a, b) => a.name.localeCompare(b.name));
          this.createForm.reset();
          this.showCreateForm = false;
          this.creating = false;
          this.showSuccess('User created successfully!');
        },
        error: (err) => {
          // HTTP 409 Conflict = email already taken
          this.error = err.status === 409
            ? 'A user with this email already exists.'
            : 'Failed to create user. Please try again.';
          this.creating = false;
        }
      });
  }

  // ── Edit ───────────────────────────────────────────────────────────────────

  startEdit(user: User): void {
    this.editingUserId = user.id;
    this.editForm.patchValue({ name: user.name, email: user.email });
  }

  cancelEdit(): void {
    this.editingUserId = null;
    this.editForm.reset();
  }

  onSave(userId: string): void {
    this.editForm.markAllAsTouched();
    if (this.editForm.invalid) return;

    this.saving = true;

    this.userService.updateUser(userId, this.editForm.value)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updated) => {
          this.users = this.users.map(u => u.id === userId ? { ...updated, taskCount: u.taskCount } : u);
          this.cancelEdit();
          this.saving = false;
          this.showSuccess('User updated successfully!');
        },
        error: (err) => {
          this.error = err.status === 409
            ? 'This email is already in use by another user.'
            : 'Failed to update user.';
          this.saving = false;
        }
      });
  }

  // ── Delete ─────────────────────────────────────────────────────────────────

  deleteUser(user: User): void {
    if (!confirm(`Delete user "${user.name}"?\n\nTheir ${user.taskCount} task(s) will become unassigned.`)) return;

    this.userService.deleteUser(user.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.users = this.users.filter(u => u.id !== user.id);
          this.showSuccess(`User "${user.name}" deleted.`);
        },
        error: () => {
          this.error = 'Failed to delete user.';
        }
      });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  private showSuccess(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => this.successMessage = null, 3000);
  }

  isFieldInvalid(form: FormGroup, field: string): boolean {
    const c = form.get(field);
    return !!(c?.invalid && c?.touched);
  }

  getFieldError(form: FormGroup, field: string): string {
    const c = form.get(field);
    if (!c?.errors) return '';
    if (c.errors['required']) return 'This field is required.';
    if (c.errors['email']) return 'Enter a valid email address.';
    if (c.errors['maxlength']) return `Too long.`;
    return 'Invalid.';
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString();
  }
}
