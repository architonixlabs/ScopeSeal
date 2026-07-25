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
    <form (ngSubmit)="search()">
      <input [(ngModel)]="query" name="query" placeholder="Tenant name or public ID" />
      <button type="submit">Search</button>
    </form>
    @if (error()) {
      <p>{{ error() }}</p>
    }
    <table>
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
        }
      </tbody>
    </table>
  `,
  styles: `
    table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 1rem;
    }
    th,
    td {
      border-bottom: 1px solid #ddd;
      text-align: left;
      padding: 0.5rem;
    }
    form {
      display: flex;
      gap: 0.5rem;
      margin-top: 1rem;
    }
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
