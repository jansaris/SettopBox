import { OnInit } from '@angular/core';
import { ModuleService } from './module.service'
import { Module, KeyblockInfo } from './models';

export abstract class ModuleBaseComponent implements OnInit {
    module: Module;
    running: boolean = false;
    loading: boolean = false;
    abstract apiName: string;

    constructor(private moduleService: ModuleService){

    }

    getStatusClass(status: string): string {
      switch (status) {
        case "Running":
        case "Idle":
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
    this.loading = true;
    this.moduleService.get(this.apiName).then(m => this.updateModule(m));
  }

  start(): void{
    this.loading = true;
    this.moduleService.start(this.apiName).then(m => {
      this.updateModule(m);
      setTimeout(()=>{
        this.loadInfo();
      }, 5000);
    });
  }

  stop(): void{
    this.loading = true;
    this.moduleService.stop(this.apiName).then(m => this.updateModule(m));
  }

  updateModule(module: Module){
      this.running = (module.Status == 'Running' || module.Status == 'Idle');
      this.module = module;
      this.loading = false;
      this.updateInfo(module);
  }


  abstract updateInfo(module: Module): void;
}