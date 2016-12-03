import { Component } from '@angular/core';
import { ModuleService } from '../module.service'
import { ModuleBaseComponent } from '../module.base.component'

import { Module, TvheadendInfo } from '../models';

@Component({
  selector: 'app-tvheadend',
  templateUrl: './tvheadend.component.html',
  styleUrls: ['./tvheadend.component.css']
})
export class TvheadendComponent extends ModuleBaseComponent {

  info: TvheadendInfo;
  
  constructor(private moduleService: ModuleService) {
    super();
  }

  loadInfo(): void {
    this.moduleService.getModule("TvHeadendIntegration").then(m => {
      this.module = m;
      this.info = m.Info as TvheadendInfo;
    });
  }
}
