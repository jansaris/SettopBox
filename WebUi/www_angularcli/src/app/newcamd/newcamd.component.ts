import { Component, OnInit } from '@angular/core';
import { ModuleService } from '../module.service'
import { ModuleBaseComponent } from '../module.base.component'

import { Module, NewcamdInfo } from '../models';

@Component({
  selector: 'app-newcamd',
  templateUrl: './newcamd.component.html',
  styleUrls: ['./newcamd.component.css']
})
export class NewcamdComponent extends ModuleBaseComponent {
  info: NewcamdInfo;
  
  constructor(private moduleService: ModuleService) {
    super();
  }

  loadInfo(): void {
    this.moduleService.getModule("NewCamd").then(m => {
      this.module = m;
      this.info = m.Info as NewcamdInfo;
    });
  }
}
