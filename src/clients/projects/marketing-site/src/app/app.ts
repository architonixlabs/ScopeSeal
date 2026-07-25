import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'ss-marketing-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="ss-shell">
      <header class="ss-shell__header">
        <div class="ss-shell__header-inner">
          <div class="ss-shell__brand">
            <h1><a routerLink="/">ScopeSeal</a></h1>
            <p class="ss-shell__tagline">
              Communication clarity for interior design and home renovation projects.
            </p>
          </div>
          <nav class="ss-nav" aria-label="Primary">
            <a class="ss-nav__link" routerLink="/features" routerLinkActive="active">Features</a>
            <a class="ss-nav__link" routerLink="/how-it-works" routerLinkActive="active">How it works</a>
            <a class="ss-nav__link" routerLink="/pricing" routerLinkActive="active">Pricing</a>
            <a class="ss-nav__link" routerLink="/security" routerLinkActive="active">Security</a>
            <a class="ss-nav__link" routerLink="/login" routerLinkActive="active">Login</a>
            <a class="ss-nav__link ss-nav__link--cta" routerLink="/register" routerLinkActive="active">
              Register
            </a>
          </nav>
        </div>
      </header>
      <main class="ss-shell__main">
        <router-outlet />
      </main>
      <footer class="ss-shell__footer">
        ScopeSeal — approval records and change control. Not legal advice.
      </footer>
    </div>
  `,
})
export class App {}
