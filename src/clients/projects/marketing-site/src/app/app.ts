import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'ss-marketing-root',
  imports: [RouterOutlet],
  template: `
    <main class="shell">
      <header>
        <h1>ScopeSeal</h1>
        <p>Communication clarity for interior design and home renovation projects.</p>
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
  `,
})
export class App {}
