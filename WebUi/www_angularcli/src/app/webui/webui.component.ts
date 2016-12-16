import { Component } from '@angular/core';
import { ModuleService } from '../module.service'
import { SettingsService } from '../settings.service'
import { ModuleBaseComponent } from '../module.base.component'
import { SettingComponent } from '../setting/setting.component'

import {Module} from '../models';

@Component({
  selector: 'app-webui',
  templateUrl: './webui.component.html',
  styleUrls: ['./webui.component.css']
})
export class WebUiComponent extends ModuleBaseComponent {
    apiName: string = "WebUi";
    constructor(moduleService: ModuleService, settingsService: SettingsService) {
      super(moduleService, settingsService);
    }

    updateInfo(m: Module): void {
    
    }
}
