/*
 * environment.ts — Development Environment Configuration
 *
 * WHY environment files:
 * Angular's build system swaps this file with environment.prod.ts during production builds.
 * This lets you have different API URLs, feature flags, and logging levels per environment
 * without changing application code.
 *
 * angular.json "fileReplacements" configuration handles the swap at build time.
 * This pattern is called "Environment-specific configuration" — critical for CI/CD pipelines
 * where the same code deploys to staging and production with different settings.
 */

export const environment = {
  production: false,
  // Local development: point to the .NET API running on localhost
  apiUrl: 'https://taskflowapi-gydh.onrender.com/api'
};
