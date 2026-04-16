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

import { Component } from '@angular/core';

/**
 * Root component — provides app shell with navigation.
 * selector: 'app-root' matches the <app-root> tag in index.html.
 */
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'TaskFlow';

  /** Controls mobile nav menu open/close state */
  menuOpen = false;

  toggleMenu(): void {
    this.menuOpen = !this.menuOpen;
  }

  closeMenu(): void {
    this.menuOpen = false;
  }
}
