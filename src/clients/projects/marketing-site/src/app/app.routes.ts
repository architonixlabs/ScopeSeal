import { Routes } from '@angular/router';
import { MarketingPageComponent } from './pages/marketing-page.component';

const page = (title: string, description: string) => ({
  title,
  description,
});

export const routes: Routes = [
  {
    path: '',
    component: MarketingPageComponent,
    data: page(
      'ScopeSeal — Communication clarity for project agreements',
      'Turn conversations and uploaded material into clear approval records, scope boundaries, and change-controlled versions.',
    ),
  },
  { path: 'features', component: MarketingPageComponent, data: page('Features', 'Agreement Snapshots, Change Ledger, review workflows, and record packages.') },
  { path: 'how-it-works', component: MarketingPageComponent, data: page('How it works', 'Capture scope, review together, approve a version, and manage changes without overwriting approved records.') },
  { path: 'use-cases', component: MarketingPageComponent, data: page('Use cases', 'Freelancers, interior designers, contractors, and event vendors.') },
  { path: 'use-cases/freelancers', component: MarketingPageComponent, data: page('Freelancers', 'Clarify deliverables, timelines, and payment milestones before work begins.') },
  { path: 'use-cases/interior-designers', component: MarketingPageComponent, data: page('Interior designers', 'Document included and excluded scope for renovation projects.') },
  { path: 'use-cases/contractors', component: MarketingPageComponent, data: page('Contractors', 'Reduce disputes with written scope boundaries and change records.') },
  { path: 'use-cases/event-vendors', component: MarketingPageComponent, data: page('Event vendors', 'Align deliverables and assumptions for weddings and corporate events.') },
  { path: 'pricing', component: MarketingPageComponent, data: page('Pricing', 'Free, Pro, and Business plans. Web checkout via Razorpay for paid plans.') },
  { path: 'security', component: MarketingPageComponent, data: page('Security', 'Tenant isolation, server-side authorization, audit events, and integrity hashes.') },
  { path: 'privacy', component: MarketingPageComponent, data: page('Privacy', 'Purpose limitation, consent records, export and deletion workflows.') },
  { path: 'ai-transparency', component: MarketingPageComponent, data: page('AI transparency', 'AI output starts as Draft. Manual review required. Default mode is ManualOnly.') },
  { path: 'subprocessors', component: MarketingPageComponent, data: page('Subprocessors', 'Current subprocessors are listed in the privacy centre API and documentation.') },
  { path: 'help', component: MarketingPageComponent, data: page('Help', 'Documentation and support resources for ScopeSeal users.') },
  { path: 'faq', component: MarketingPageComponent, data: page('FAQ', 'Common questions about plans, approvals, and record packages.') },
  { path: 'contact', component: MarketingPageComponent, data: page('Contact', 'Reach the ScopeSeal team for product and support enquiries.') },
  { path: 'blog', component: MarketingPageComponent, data: page('Blog', 'Learning resources about scope clarity and change control.') },
  { path: 'download', component: MarketingPageComponent, data: page('Download', 'Free companion apps for Android and iOS. Paid upgrades are managed on the web.') },
  { path: 'legal/privacy', component: MarketingPageComponent, data: page('Privacy policy', 'Draft policy — requires qualified legal review before publication.') },
  { path: 'legal/terms', component: MarketingPageComponent, data: page('Terms of service', 'Draft terms — requires qualified legal review before publication.') },
  { path: 'legal/acceptable-use', component: MarketingPageComponent, data: page('Acceptable use', 'Draft acceptable use policy — requires qualified legal review.') },
  { path: 'legal/refund-and-cancellation', component: MarketingPageComponent, data: page('Refund and cancellation', 'Draft refund policy — requires qualified legal review.') },
  { path: 'login', component: MarketingPageComponent, data: page('Login', 'Sign in to the ScopeSeal product application.') },
  { path: 'register', component: MarketingPageComponent, data: page('Register', 'Create a free ScopeSeal account.') },
  { path: '**', redirectTo: '' },
];
