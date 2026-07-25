import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminAuthService } from '../core/admin-auth.service';

@Component({
  selector: 'ss-admin-login',
  imports: [FormsModule],
  template: `
    <section class="ss-card ss-panel">
      <div class="ss-card__body">
        <h1>Operator sign-in</h1>
        <p>Restricted administration portal. Metadata-only views — no customer document access.</p>
        <form (ngSubmit)="submit()">
          <div class="ss-field">
            <label for="operator-key">Platform operator key</label>
            <input
              id="operator-key"
              class="ss-input"
              type="password"
              [(ngModel)]="key"
              name="key"
              required
              autocomplete="off"
            />
          </div>
          @if (error()) {
            <p class="ss-error">{{ error() }}</p>
          }
          <button type="submit" class="ss-btn ss-btn--primary">Continue</button>
        </form>
      </div>
    </section>
  `,
})
export class LoginComponent {
  private readonly auth = inject(AdminAuthService);
  private readonly router = inject(Router);

  key = '';
  readonly error = signal<string | null>(null);

  submit(): void {
    if (!this.key.trim()) {
      this.error.set('Operator key is required.');
      return;
    }

    this.auth.signIn(this.key);
    void this.router.navigateByUrl('/dashboard');
  }
}
