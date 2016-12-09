/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { PerformanceService } from './performance.service';

describe('PerformanceService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PerformanceService]
    });
  });

  it('should ...', inject([PerformanceService], (service: PerformanceService) => {
    expect(service).toBeTruthy();
  }));
});
