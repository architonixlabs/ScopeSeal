import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AdminAuthService } from '../core/admin-auth.service';

@Component({
  selector: 'ss-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="ss-admin-layout">
      <nav class="ss-admin-nav" aria-label="Admin navigation">
        <h1>ScopeSeal Admin</h1>
        <p class="notice ss-notice--info">
          Metadata-only operator views. Customer content is not accessible.
        </p>
        <a class="ss-nav__link" routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
        <a class="ss-nav__link" routerLink="/tenants" routerLinkActive="active">Tenants</a>
        <a class="ss-nav__link" routerLink="/privacy-queue" routerLinkActive="active">Privacy queue</a>
        <a class="ss-nav__link" routerLink="/feature-flags" routerLinkActive="active">Feature flags</a>
        <button type="button" class="ss-btn ss-btn--ghost" (click)="signOut()">Sign out</button>
      </nav>
      <main class="ss-admin-main">
        <router-outlet />
      </main>
    </div>
  `,
})
export class AdminShellComponent {
  private readonly auth = inject(AdminAuthService);

  signOut(): void {
    this.auth.signOut();
    location.href = '/login';
  }
}
