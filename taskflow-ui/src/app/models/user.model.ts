/*
 * user.model.ts — TypeScript Interface for User Data
 *
 * Architectural Pattern: Domain Model (TypeScript Interface)
 *
 * WHY: Mirrors the C# UserResponseDto from the backend.
 * Provides compile-time type safety for user data throughout the Angular app.
 */

/**
 * Matches the UserResponseDto returned by GET /api/users endpoints.
 */
export interface User {
  id: string;
  name: string;
  email: string;
  createdAt: string;
  taskCount: number;
}

/**
 * Matches the CreateUserDto accepted by POST /api/users.
 */
export interface CreateUserRequest {
  name: string;
  email: string;
}

/**
 * Matches the UpdateUserDto accepted by PUT /api/users/{id}.
 */
export interface UpdateUserRequest {
  name?: string;
  email?: string;
}
