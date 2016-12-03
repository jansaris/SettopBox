/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { EpgComponent } from './epg.component';

describe('EpgComponent', () => {
  let component: EpgComponent;
  let fixture: ComponentFixture<EpgComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ EpgComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(EpgComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
