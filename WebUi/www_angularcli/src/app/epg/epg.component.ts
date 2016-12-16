import { Component } from '@angular/core';
import { ModuleService } from '../module.service'
import { SettingsService } from '../settings.service'
import { ModuleBaseComponent } from '../module.base.component'
import { SettingComponent } from '../setting/setting.component'

import { Module, EpgInfo } from '../models';

@Component({
  selector: 'app-epg',
  templateUrl: './epg.component.html',
  styleUrls: ['./epg.component.css']
})
export class EpgComponent extends ModuleBaseComponent {
  apiName: string = "EpgGrabber";
  info: EpgInfo;

  constructor(moduleService: ModuleService, settingsService: SettingsService) {
    super(moduleService, settingsService);
  }

  updateInfo(m: Module): void {
    this.info = m.Info as EpgInfo;
  }
}