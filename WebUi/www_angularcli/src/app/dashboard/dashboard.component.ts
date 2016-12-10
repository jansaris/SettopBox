import { Component, OnInit, OnDestroy } from '@angular/core';
import { ModuleService } from '../module.service';
import { HomeService } from '../home.service';
import { PerformanceService } from '../performance.service';
import { Module, Performance } from '../models';

import { IntervalObservable } from 'rxjs/Observable/IntervalObservable';
import { Subscription } from 'rxjs/Subscription';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
    modules: Module[];
    message: string;
    performance: Performance;
    monitor: Subscription;
    running: boolean = false;
    
    constructor(private moduleService: ModuleService, private homeService: HomeService, private performanceService: PerformanceService ) {}

    ngOnInit(): void {
        this.getHome();
        this.getModules();
        this.startMonitoring();
    };

    ngOnDestroy(): void{
        this.stopMonitoring();
    }

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

    updatePerformance(): void{
        this.performanceService.get().then(perf => {
            this.performance = perf;
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

    toggleMonitor(): void {
        if(this.running) this.stopMonitoring();
        else this.startMonitoring();
    }

    startMonitoring(): void{
        if(this.running) return;
        this.monitor = IntervalObservable.create(1000).subscribe(n => this.updatePerformance());
        this.running = true;
    }

    stopMonitoring(): void{
        if(!this.running) return;
        this.monitor.unsubscribe();
        this.running = false;
    }
}
