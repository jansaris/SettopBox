import { Component, OnInit } from '@angular/core';
import { ModuleService } from './module.service';
import { ErrorService } from './error.service';
import { Module } from './models';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent {
    title = 'Settop-box';
    error = '';
    modules: Module[];
    
    constructor(private moduleService: ModuleService, private errorService : ErrorService) { }
    
    ngOnInit(): void {
        this.getModules();
        this.errorService.errorOccured.subscribe(this.updateError);
        this.error = this.errorService.lastError;
        //this.errorService.subscribe(this.updateError);
    };

    getModules(): void {
        this.moduleService.getModules().then(modules => {
            this.modules = modules;
        });
    }

    updateError(error: any): void {
      this.error = error.value;
    }
}
