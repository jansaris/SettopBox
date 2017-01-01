import { OnInit } from '@angular/core';
import { ModuleService } from './module.service'
import { SettingsService } from './settings.service'
import { Module, KeyblockInfo } from './models';

import { Setting } from './models'

export abstract class ModuleBaseComponent implements OnInit {
    module: Module;
    settings: Setting[];
    running: boolean = false;
    loading: boolean = false;
    abstract apiName: string;

    constructor(protected moduleService: ModuleService, private settingsService: SettingsService){

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
    this.loadSettings();
    this.loadData();
  }

  loadInfo(): void{
    this.loading = true;
    this.moduleService.get(this.apiName).then(m => this.updateModule(m));
  }

  loadSettings(): void{
    this.loading = true;
    this.settingsService.get(this.apiName).then(s => {
      this.settings = s;
      this.loading = false;
    });
  }

  getNrOfChangedSettings(): number {
    var count = 0;
    for (var index in this.settings) {
      var set = this.settings[index];
      if(set.Value != set.ServerValue){
        count++;
      }
    }
    return count;
  }

  saveSettings(): void{
    this.loading = true;
    this.settingsService.put(this.apiName, this.settings).then(changes => {
      if(changes){
        this.loadSettings();  
      }
      else{
        this.loading = false;
      }
    });
  }

  resetSettings(): void{
    for (var index in this.settings) {
      var set = this.settings[index];
      set.Value = set.ServerValue;
    }
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

  loadData(): void {

  };

  abstract updateInfo(module: Module): void;
  
}