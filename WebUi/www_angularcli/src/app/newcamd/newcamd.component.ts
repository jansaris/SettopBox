import { Component, OnInit } from '@angular/core';
import { ModuleService } from '../module.service'
import { SettingsService } from '../settings.service'
import { ModuleBaseComponent } from '../module.base.component'
import { SettingComponent } from '../setting/setting.component'

import { Module, NewcamdInfo } from '../models';

@Component({
  selector: 'app-newcamd',
  templateUrl: './newcamd.component.html',
  styleUrls: ['./newcamd.component.css']
})
export class NewcamdComponent extends ModuleBaseComponent {
  info: NewcamdInfo;
  apiName: string = "NewCamd";
  
  constructor(moduleService: ModuleService, settingsService: SettingsService) {
    super(moduleService, settingsService);
  }

  updateInfo(module: Module) {
      this.info = module.Info as NewcamdInfo;
  }
}
