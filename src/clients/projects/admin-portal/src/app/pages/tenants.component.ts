import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../core/admin-api.service';

interface TenantRow {
  publicId: string;
  name: string;
  createdAtUtc: string;
  memberCount: number;
  currentPlanCode: string;
}

@Component({
  selector: 'ss-admin-tenants',
  imports: [FormsModule, DatePipe],
  template: `
    <h2>Tenant search</h2>
    <p>Metadata-only listing — no workspace or document content.</p>
    <form class="ss-form-row" (ngSubmit)="search()">
      <input
        class="ss-input"
        [(ngModel)]="query"
        name="query"
        placeholder="Tenant name or public ID"
      />
      <button type="submit" class="ss-btn ss-btn--primary">Search</button>
    </form>
    @if (error()) {
      <p class="ss-error">{{ error() }}</p>
    }
    <div class="ss-table-wrap">
      <table class="ss-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Plan</th>
            <th>Members</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          @for (tenant of tenants(); track tenant.publicId) {
            <tr>
              <td>{{ tenant.name }}</td>
              <td>{{ tenant.currentPlanCode }}</td>
              <td>{{ tenant.memberCount }}</td>
              <td>{{ tenant.createdAtUtc | date: 'medium' }}</td>
            </tr>
          } @empty {
            <tr>
              <td colspan="4">
                <div class="ss-empty">
                  <p class="ss-empty__title">No tenants found</p>
                  <p>Try a different search term.</p>
                </div>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class TenantsComponent {
  private readonly api = inject(AdminApiService);

  query = '';
  readonly tenants = signal<TenantRow[]>([]);
  readonly error = signal<string | null>(null);

  async search(): Promise<void> {
    this.error.set(null);
    try {
      const encoded = encodeURIComponent(this.query.trim());
      const result = await this.api.get<{ items: TenantRow[] }>(
        `/api/v1/admin/tenants/search?q=${encoded}`,
      );
      this.tenants.set(result.items);
    } catch {
      this.error.set('Tenant search failed.');
    }
  }
}
