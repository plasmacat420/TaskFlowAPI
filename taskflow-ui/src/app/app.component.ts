/*
 * app.component.ts — Root Application Component
 *
 * Architectural Pattern: Shell Component (Layout Component)
 *
 * WHY a root shell component:
 * AppComponent is the outermost component — it defines the persistent layout
 * (navigation bar, sidebar) that wraps all routed views.
 * Child components are rendered inside <router-outlet> based on the current URL.
 *
 * This is the "Shell" pattern: a persistent chrome (nav) + a dynamic content area (outlet).
 * The shell never re-renders; only the routed component changes on navigation.
 */

import { Component, OnInit } from '@angular/core';
import { TaskService } from './services/task.service';

/**
 * Root component — provides app shell with navigation.
 * selector: 'app-root' matches the <app-root> tag in index.html.
 */
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  title = 'TaskFlow';
  menuOpen = false;

  /**
   * Shows a banner while the Render API is cold-starting.
   * Render free tier spins down after 15 min of inactivity; the first
   * request after sleep gets a connection error. We ping on app load so
   * the service is warm before the user tries to submit a form.
   */
  apiConnecting = false;
  apiError = false;

  constructor(private taskService: TaskService) {}

  ngOnInit(): void {
    // Fire a ping immediately so Render wakes up before the user does anything.
    // If the API is already warm this completes instantly (< 1s).
    // If cold, Render needs ~30s; we show a banner so the user knows to wait.
    this.apiConnecting = true;
    this.taskService.getAllTasks().subscribe({
      next:  ()  => { this.apiConnecting = false; this.apiError = false; },
      error: (e) => {
        this.apiConnecting = false;
        // status 0 = no connection (cold start / network down)
        this.apiError = e.status === 0;
      }
    });
  }

  toggleMenu(): void { this.menuOpen = !this.menuOpen; }
  closeMenu():  void { this.menuOpen = false; }
}
