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
  apiName: string = "TvHeadendIntegration";
  info: TvheadendInfo;
  
  constructor(moduleService: ModuleService) {
    super(moduleService);
  }

  updateInfo(m: Module): void {
    this.info = m.Info as TvheadendInfo;
  }
}
