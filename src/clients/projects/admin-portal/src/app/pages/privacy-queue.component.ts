import { Component, inject, OnInit, signal } from '@angular/core';
import { AdminApiService } from '../core/admin-api.service';

interface QueueItem {
  publicId: string;
  queueStatus: string;
  assignedOperator?: string;
}

@Component({
  selector: 'ss-admin-privacy-queue',
  template: `
    <h2>Privacy operator queue</h2>
    <p>Queue metadata only — request subjects, not exported payloads.</p>
    @if (error()) {
      <p>{{ error() }}</p>
    } @else {
      <ul>
        @for (item of items(); track item.publicId) {
          <li>{{ item.publicId }} — {{ item.queueStatus }}</li>
        } @empty {
          <li>No queue items.</li>
        }
      </ul>
    }
  `,
})
export class PrivacyQueueComponent implements OnInit {
  private readonly api = inject(AdminApiService);

  readonly items = signal<QueueItem[]>([]);
  readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      const result = await this.api.get<{ items: QueueItem[] }>('/api/v1/admin/privacy/queue');
      this.items.set(result.items);
    } catch {
      this.error.set('Unable to load privacy queue.');
    }
  }
}
