/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { KeyblockComponent } from './keyblock.component';

describe('KeyblockComponent', () => {
  let component: KeyblockComponent;
  let fixture: ComponentFixture<KeyblockComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ KeyblockComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(KeyblockComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
