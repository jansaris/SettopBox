import { OnInit } from '@angular/core';

import {Module, KeyblockInfo} from './models';

export abstract class ModuleBaseComponent implements OnInit {
    module: Module;

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

  ngOnInit() {
    this.loadInfo();
  }

  abstract loadInfo(): void;
}