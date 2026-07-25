import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'ss-root',
  imports: [RouterOutlet],
  template: `
    <div class="ss-shell">
      <header class="ss-shell__header">
        <div class="ss-shell__header-inner">
          <div class="ss-shell__brand">
            <h1>ScopeSeal</h1>
            <p class="ss-shell__tagline">Agreement snapshots, approvals, and change control.</p>
          </div>
        </div>
      </header>
      <main class="ss-shell__main">
        <router-outlet />
      </main>
    </div>
  `,
})
export class App {}
