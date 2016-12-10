import { Component, OnInit } from '@angular/core';
import { ModuleService } from '../module.service'
import { LogService } from '../log.service'

import { Log } from '../models';

@Component({
  selector: 'app-log',
  templateUrl: './log.component.html',
  styleUrls: ['./log.component.css']
})
export class LogComponent implements OnInit {
  list: Log[];
  levels: string[];
  modules: string[];
  all: string = "ALL";
  activeLevel: string = this.all;
  activeModule: string = this.all;
  loading: boolean = true;

  constructor(private moduleService: ModuleService, private logService: LogService) { }

  ngOnInit() {
    this.loadLevels();
    this.loadModuleNames();
    this.loadLog();
  }

  loadLevels(): void {
    this.logService
      .getLevels()
      .then(list => {
        this.levels = list;
      });
  }

  loadModuleNames(): void {
    this.moduleService
      .getNames()
      .then(list => {
        this.modules = list;
      });
  }

  loadLog(): void {
    this.loading = true;
    this.logService
      .get(this.activeModule, this.activeLevel)
      .then(logs => {
        this.list = logs;
        this.loading = false;
      });
  }

  changeLevel(level: string): void {
    this.activeLevel = level;
    this.loadLog();
  }

  changeModule(module: string): void {
    this.activeModule = module;
    this.loadLog();
  }
}
