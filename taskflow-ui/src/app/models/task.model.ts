/*
 * task.model.ts — TypeScript Interface for Task Data
 *
 * Architectural Pattern: Domain Model (TypeScript Interface)
 *
 * WHY TypeScript interfaces for models:
 * These interfaces mirror the C# DTOs from the backend API.
 * TypeScript's structural typing ensures that API responses are typed correctly —
 * if the backend changes the response shape, TypeScript compilation will catch mismatches.
 *
 * In a larger project, these would be auto-generated from the OpenAPI spec using tools
 * like NSwag or the OpenAPI Generator. For this showcase, they're hand-written to be explicit.
 *
 * WHY interfaces vs classes for models:
 * Interfaces are compile-time only (zero runtime cost), ideal for data shapes.
 * Classes are needed when you need methods or decorators on the object.
 * API response data (plain JSON) maps naturally to interfaces.
 */

/**
 * Matches the TaskResponseDto returned by GET /api/tasks endpoints.
 * All fields are read-only from the API perspective — mutations go through
 * CreateTaskRequest and UpdateTaskRequest.
 */
export interface Task {
  id: string;           // Guid as string in JSON
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate: string | null;  // ISO 8601 UTC string from the API
  createdAt: string;       // ISO 8601 UTC string
  assignedUserId: string | null;
  assignedUserName: string | null;
}

/**
 * Matches the CreateTaskDto accepted by POST /api/tasks.
 * Only fields the client is allowed to set on creation.
 */
export interface CreateTaskRequest {
  title: string;
  description?: string | null;
  status?: TaskStatus;
  priority?: TaskPriority;
  dueDate?: string | null;
  assignedUserId?: string | null;
}

/**
 * Matches the UpdateTaskDto accepted by PUT /api/tasks/{id}.
 * All fields optional — partial update semantics.
 */
export interface UpdateTaskRequest {
  title?: string;
  description?: string | null;
  status?: TaskStatus;
  priority?: TaskPriority;
  dueDate?: string | null;
  assignedUserId?: string | null;
}

/**
 * Task workflow statuses — must match the C# TaskItemStatus enum string values.
 * The .NET API serializes enums as strings (configured in Program.cs AddJsonOptions).
 */
export type TaskStatus = 'Pending' | 'InProgress' | 'Done';

/**
 * Task priority levels — must match the C# TaskPriority enum string values.
 */
export type TaskPriority = 'Low' | 'Medium' | 'High';

/** All status options for use in dropdowns and filters */
export const TASK_STATUSES: TaskStatus[] = ['Pending', 'InProgress', 'Done'];

/** All priority options for use in dropdowns and filters */
export const TASK_PRIORITIES: TaskPriority[] = ['Low', 'Medium', 'High'];
