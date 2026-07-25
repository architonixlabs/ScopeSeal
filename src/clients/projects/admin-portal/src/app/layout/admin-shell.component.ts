import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AdminAuthService } from '../core/admin-auth.service';

@Component({
  selector: 'ss-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="layout">
      <nav aria-label="Admin navigation">
        <h1>ScopeSeal Admin</h1>
        <p class="notice">Metadata-only operator views. Customer content is not accessible.</p>
        <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
        <a routerLink="/tenants" routerLinkActive="active">Tenants</a>
        <a routerLink="/privacy-queue" routerLinkActive="active">Privacy queue</a>
        <a routerLink="/feature-flags" routerLinkActive="active">Feature flags</a>
        <button type="button" (click)="signOut()">Sign out</button>
      </nav>
      <main>
        <router-outlet />
      </main>
    </div>
  `,
  styles: `
    .layout {
      display: grid;
      grid-template-columns: 14rem 1fr;
      min-height: 100vh;
    }
    nav {
      padding: 1rem;
      border-right: 1px solid #ddd;
      display: grid;
      gap: 0.5rem;
      align-content: start;
    }
    .notice {
      font-size: 0.85rem;
      color: #555;
    }
    a.active {
      font-weight: 600;
    }
    main {
      padding: 1.5rem;
    }
  `,
})
export class AdminShellComponent {
  private readonly auth = inject(AdminAuthService);

  signOut(): void {
    this.auth.signOut();
    location.href = '/login';
  }
}
