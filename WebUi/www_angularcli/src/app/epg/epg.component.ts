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
  info: EpgInfo;

  constructor(private moduleService: ModuleService) {
    super();
  }

  loadInfo(): void{
    this.moduleService.getModule("EpgGrabber").then(m => {
      this.module = m;
      this.info = m.Info as EpgInfo;
    });
  }
}
