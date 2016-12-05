import { Component, OnInit } from '@angular/core';
import { ModuleService } from '../module.service';
import { HomeService } from '../home.service';
import {Module} from '../models';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
    modules: Module[];
    message: string;
    
    constructor(private moduleService: ModuleService, private homeService: HomeService) {}

    ngOnInit(): void {
        this.getModules();
    };

    getHome(): void {
      this.homeService.get().then(response => {
        this.message = response;
      });
    }

    getModules(): void {
        this.moduleService.getAll().then(modules => {
            this.modules = modules;
        });
    }

    getStatusClass(status: string): string {
        switch (status) {
            case "Running":
                return "alert-success";
            case "Disabled":
                return "alert-warning";
            default:
                return "alert-info";
        }
    }
}
