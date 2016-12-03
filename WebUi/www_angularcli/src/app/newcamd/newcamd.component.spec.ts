/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { NewcamdComponent } from './newcamd.component';

describe('NewcamdComponent', () => {
  let component: NewcamdComponent;
  let fixture: ComponentFixture<NewcamdComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NewcamdComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NewcamdComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
