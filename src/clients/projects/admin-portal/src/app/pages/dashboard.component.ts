import { Component, inject, OnInit, signal } from '@angular/core';
import { AdminApiService } from '../core/admin-api.service';

@Component({
  selector: 'ss-admin-dashboard',
  template: `
    <h2>Platform dashboard</h2>
    @if (loading()) {
      <p>Loading metadata…</p>
    } @else if (error()) {
      <p>{{ error() }}</p>
    } @else {
      <ul>
        <li>Feature flags: {{ featureFlagCount() }}</li>
        <li>Open grievances: {{ grievanceCount() }}</li>
        <li>Dead-letter jobs: {{ deadLetterCount() }}</li>
      </ul>
    }
  `,
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(AdminApiService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly featureFlagCount = signal(0);
  readonly grievanceCount = signal(0);
  readonly deadLetterCount = signal(0);

  async ngOnInit(): Promise<void> {
    try {
      const [flags, grievances, deadLetter] = await Promise.all([
        this.api.get<{ items: unknown[] }>('/api/v1/admin/feature-flags'),
        this.api.get<{ items: unknown[] }>('/api/v1/admin/privacy/grievances'),
        this.api.get<{ items: unknown[] }>('/api/v1/admin/jobs/dead-letter'),
      ]);

      this.featureFlagCount.set(flags.items.length);
      this.grievanceCount.set(grievances.items.length);
      this.deadLetterCount.set(deadLetter.items.length);
    } catch {
      this.error.set('Unable to load dashboard metadata.');
    } finally {
      this.loading.set(false);
    }
  }
}
