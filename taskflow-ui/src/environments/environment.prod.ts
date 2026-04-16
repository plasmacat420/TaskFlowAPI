/*
 * environment.prod.ts — Production Environment Configuration
 *
 * WHY: Swapped in at build time by angular.json when building with --configuration production.
 * Points to the Render-deployed backend instead of localhost.
 *
 * IMPORTANT: Replace the apiUrl with your actual Render service URL after deployment.
 * Format: https://your-service-name.onrender.com/api
 */

export const environment = {
  production: true,
  // Replace with your actual Render backend URL after deploying
  apiUrl: 'https://taskflowapi-gydh.onrender.com/api'
};
