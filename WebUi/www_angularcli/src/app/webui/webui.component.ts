import { Component, OnInit } from '@angular/core';
import { ModuleService } from '../module.service'

@Component({
  selector: 'app-webui',
  templateUrl: './webui.component.html',
  styleUrls: ['./webui.component.css']
})
export class WebUiComponent implements OnInit {
module: IModule;

    constructor(private moduleService: ModuleService) {
        this.module = null;
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
