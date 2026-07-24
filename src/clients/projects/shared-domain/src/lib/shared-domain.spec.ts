import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SharedDomain } from './shared-domain';

describe('SharedDomain', () => {
  let component: SharedDomain;
  let fixture: ComponentFixture<SharedDomain>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SharedDomain],
    }).compileComponents();

    fixture = TestBed.createComponent(SharedDomain);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
