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
  apiName: string = "Keyblock";
  info: KeyblockInfo;

  constructor(moduleService: ModuleService) {
    super(moduleService);
  }

  updateInfo(m: Module): void {
    this.info = m.Info as KeyblockInfo;
  }
}
