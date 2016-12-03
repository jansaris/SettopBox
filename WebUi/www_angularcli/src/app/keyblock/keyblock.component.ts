import { Component } from '@angular/core';
import { ModuleService } from '../module.service'
import { ModuleBaseComponent } from '../module.base.component'

import {Module, KeyblockInfo} from '../models';

@Component({
  selector: 'app-keyblock',
  templateUrl: './keyblock.component.html',
  styleUrls: ['./keyblock.component.css']
})
export class KeyblockComponent extends ModuleBaseComponent {
  info: KeyblockInfo;
  panelColor: string;

  constructor(private moduleService: ModuleService) {
    super();
  }

  loadInfo(): void {
    this.moduleService.getModule("Keyblock").then(m => {
      this.module = m;
      this.info = m.Info as KeyblockInfo;
    });
  }
}
