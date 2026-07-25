import { Component, inject, OnInit, signal } from '@angular/core';
import { AdminApiService } from '../core/admin-api.service';

interface FeatureFlag {
  key: string;
  isEnabled: boolean;
  description: string;
}

@Component({
  selector: 'ss-admin-feature-flags',
  template: `
    <h2>Feature flags</h2>
    @if (error()) {
      <p>{{ error() }}</p>
    } @else {
      <ul>
        @for (flag of flags(); track flag.key) {
          <li>
            <strong>{{ flag.key }}</strong> — {{ flag.isEnabled ? 'Enabled' : 'Disabled' }}
            <button type="button" (click)="toggle(flag)">Toggle</button>
            <p>{{ flag.description }}</p>
          </li>
        }
      </ul>
    }
  `,
})
export class FeatureFlagsComponent implements OnInit {
  private readonly api = inject(AdminApiService);

  readonly flags = signal<FeatureFlag[]>([]);
  readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async toggle(flag: FeatureFlag): Promise<void> {
    try {
      await this.api.put(`/api/v1/admin/feature-flags/${flag.key}`, {
        isEnabled: !flag.isEnabled,
        description: flag.description,
      });
      await this.load();
    } catch {
      this.error.set('Feature flag update failed.');
    }
  }

  private async load(): Promise<void> {
    try {
      const result = await this.api.get<{ items: FeatureFlag[] }>('/api/v1/admin/feature-flags');
      this.flags.set(result.items);
    } catch {
      this.error.set('Unable to load feature flags.');
    }
  }
}
