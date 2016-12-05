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
  apiName: string = "NewCamd";
  
  constructor(moduleService: ModuleService) {
    super(moduleService);
  }

  updateInfo(module: Module) {
      this.info = module.Info as NewcamdInfo;
  }
}
