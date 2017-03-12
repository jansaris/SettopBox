import { TestBed, inject } from '@angular/core/testing';

import { SettopboxService } from './settopbox.service';

describe('SettopboxService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SettopboxService]
    });
  });

  it('should ...', inject([SettopboxService], (service: SettopboxService) => {
    expect(service).toBeTruthy();
  }));
});
