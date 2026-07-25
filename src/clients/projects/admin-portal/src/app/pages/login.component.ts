import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminAuthService } from '../core/admin-auth.service';

@Component({
  selector: 'ss-admin-login',
  imports: [FormsModule],
  template: `
    <section class="panel">
      <h1>Operator sign-in</h1>
      <p>Restricted administration portal. Metadata-only views — no customer document access.</p>
      <form (ngSubmit)="submit()">
        <label>
          Platform operator key
          <input type="password" [(ngModel)]="key" name="key" required autocomplete="off" />
        </label>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
        <button type="submit">Continue</button>
      </form>
    </section>
  `,
  styles: `
    .panel {
      max-width: 28rem;
      margin: 4rem auto;
      padding: 1.5rem;
      border: 1px solid #ddd;
      border-radius: 8px;
    }
    label {
      display: grid;
      gap: 0.5rem;
      margin-bottom: 1rem;
    }
    input {
      padding: 0.5rem;
    }
    .error {
      color: #b00020;
    }
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
