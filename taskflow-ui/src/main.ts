/*
 * main.ts — Angular Application Bootstrap Entry Point
 *
 * Architectural Pattern: Application Bootstrap / Composition Root
 *
 * WHY this file:
 * main.ts is the entry point for the Angular application.
 * It bootstraps the root NgModule (AppModule), which in turn declares all components,
 * imports all required Angular modules, and sets up the dependency injection container.
 *
 * This mirrors Program.cs in the .NET backend — both are "composition roots"
 * where the application is wired together and started.
 *
 * In Angular 17+, standalone components are the modern default.
 * We use the module-based approach (NgModule) here because it's still the
 * dominant pattern in enterprise Angular codebases and most interviews will ask about it.
 */

import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app.module';

platformBrowserDynamic()
  .bootstrapModule(AppModule)
  .catch(err => console.error(err));
