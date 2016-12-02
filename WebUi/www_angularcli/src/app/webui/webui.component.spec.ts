/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { WebuiComponent } from './webui.component';

describe('WebuiComponent', () => {
  let component: WebuiComponent;
  let fixture: ComponentFixture<WebuiComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ WebuiComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(WebuiComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
