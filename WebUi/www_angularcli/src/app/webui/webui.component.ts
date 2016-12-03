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

    constructor(private moduleService: ModuleService) {
      super();
    }

    loadInfo(): void {
        this.moduleService.getModule('WebUi').then(module => {
            this.module = module;
        });
    }
}
