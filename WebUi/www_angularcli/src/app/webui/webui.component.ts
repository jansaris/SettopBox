import { Component } from '@angular/core';
import { ModuleService } from '../module.service'
import { ModuleBaseComponent } from '../module.base.component'

import {Module} from '../models';

@Component({
  selector: 'app-webui',
  templateUrl: './webui.component.html',
  styleUrls: ['./webui.component.css']
})
export class WebUiComponent extends ModuleBaseComponent {
    apiName: string = "WebUi";
    constructor(moduleService: ModuleService) {
      super(moduleService);
    }

    updateInfo(m: Module): void {
    
    }
}
