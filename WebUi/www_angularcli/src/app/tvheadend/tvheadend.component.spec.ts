/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { TvheadendComponent } from './tvheadend.component';

describe('TvheadendComponent', () => {
  let component: TvheadendComponent;
  let fixture: ComponentFixture<TvheadendComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TvheadendComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TvheadendComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
