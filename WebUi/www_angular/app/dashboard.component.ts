import { Component, OnInit } from '@angular/core';

import { ModuleService } from './module.service'

@Component({
  moduleId: module.id,
  selector: 'dashboard',
  templateUrl: 'dashboard.component.html',
  styleUrls: ['dashboard.component.css'],
})

export class DashboardComponent implements OnInit {
    modules: IModule[];

    constructor(private moduleService: ModuleService) {}

    ngOnInit(): void {
        this.getModules();
    };

    getModules(): void {
        this.moduleService.getModules().then(modules => {
            this.modules = modules;
        });
    }
 }