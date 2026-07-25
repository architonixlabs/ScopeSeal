import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'ss-marketing-root',
  imports: [RouterOutlet, RouterLink],
  template: `
    <main class="shell">
      <header>
        <h1><a routerLink="/">ScopeSeal</a></h1>
        <p>Communication clarity for interior design and home renovation projects.</p>
        <nav aria-label="Primary">
          <a routerLink="/features">Features</a>
          <a routerLink="/pricing">Pricing</a>
          <a routerLink="/security">Security</a>
          <a routerLink="/login">Login</a>
          <a routerLink="/register">Register</a>
        </nav>
      </header>
      <router-outlet />
    </main>
  `,
  styles: `
    .shell {
      font-family: system-ui, sans-serif;
      margin: 2rem auto;
      max-width: 48rem;
      padding: 0 1rem;
    }
    nav {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;
      margin-top: 1rem;
    }
    nav a {
      color: #0b5;
    }
  `,
})
export class App {}
