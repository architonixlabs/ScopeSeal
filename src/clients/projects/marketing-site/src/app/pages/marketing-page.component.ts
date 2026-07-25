import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

export interface MarketingPageData {
  title: string;
  description: string;
}

@Component({
  selector: 'ss-marketing-page',
  template: `
    <article class="ss-page">
      <h1 class="ss-page__title">{{ page.title }}</h1>
      <p class="ss-page__lead">{{ page.description }}</p>
      <p class="notice">
        Public marketing content — requires qualified legal review before launch approval.
      </p>
    </article>
  `,
})
export class MarketingPageComponent {
  private readonly route = inject(ActivatedRoute);
  readonly page = this.route.snapshot.data as MarketingPageData;
}
