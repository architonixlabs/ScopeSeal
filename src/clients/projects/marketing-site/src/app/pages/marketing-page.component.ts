import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

export interface MarketingPageData {
  title: string;
  description: string;
}

@Component({
  selector: 'ss-marketing-page',
  template: `
    <article>
      <h1>{{ page.title }}</h1>
      <p>{{ page.description }}</p>
      <p class="notice">Public marketing content — requires qualified legal review before launch approval.</p>
    </article>
  `,
  styles: `
    article {
      margin-top: 1.5rem;
    }
    .notice {
      color: #555;
      font-size: 0.9rem;
      margin-top: 2rem;
    }
  `,
})
export class MarketingPageComponent {
  private readonly route = inject(ActivatedRoute);
  readonly page = this.route.snapshot.data as MarketingPageData;
}
