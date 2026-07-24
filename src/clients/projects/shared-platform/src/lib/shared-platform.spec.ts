import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SharedPlatform } from './shared-platform';

describe('SharedPlatform', () => {
  let component: SharedPlatform;
  let fixture: ComponentFixture<SharedPlatform>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SharedPlatform],
    }).compileComponents();

    fixture = TestBed.createComponent(SharedPlatform);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
