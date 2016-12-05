import { OnInit } from '@angular/core';
import { ModuleService } from './module.service'
import { Module, KeyblockInfo } from './models';

export abstract class ModuleBaseComponent implements OnInit {
    module: Module;
    running: boolean = false;
    abstract apiName: string;

    constructor(private moduleService: ModuleService){

    }

    getStatusClass(status: string): string {
      switch (status) {
        case "Running":
            return "panel-success";
        case "Disabled":
            return "panel-warning";
        default:
            return "panel-info";
    }
  }

  ngOnInit(): void {
    this.loadInfo();
  }

  loadInfo(): void{
    this.moduleService.get(this.apiName).then(m => this.updateModule(m));
  }

  start(): void{
    this.moduleService.start(this.apiName).then(m => this.updateModule(m));
  }

  stop(): void{
    this.moduleService.stop(this.apiName).then(m => this.updateModule(m));
  }

  updateModule(module: Module){
      this.running = module.Status == 'Running';
      this.module = module;
      this.updateInfo(module);
  }

  abstract updateInfo(module: Module): void;
}