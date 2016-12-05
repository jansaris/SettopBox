import { Component } from '@angular/core';
import { ModuleService } from '../module.service'
import { ModuleBaseComponent } from '../module.base.component'

import { Module, EpgInfo } from '../models';

@Component({
  selector: 'app-epg',
  templateUrl: './epg.component.html',
  styleUrls: ['./epg.component.css']
})
export class EpgComponent extends ModuleBaseComponent {
  apiName: string = "Keyblock";
  info: EpgInfo;

  constructor(moduleService: ModuleService) {
    super(moduleService);
  }

  updateInfo(m: Module): void {
    this.info = m.Info as EpgInfo;
  }
}