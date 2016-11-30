import { Component, OnInit } from '@angular/core';

import { ModuleService } from './module.service'

@Component({
  moduleId: module.id,
  selector: 'webui',
  templateUrl: 'webui.component.html'
})

export class WebUiComponent implements OnInit {
    module: IModule;

    constructor(private moduleService: ModuleService) {
        this.module = {} as IModule;
    }

    ngOnInit(): void {
        this.getModule();
    };

    getModule(): void {
        this.moduleService.getModule('WebUi').then(module => {
            this.module = module;
        });
    }
 }