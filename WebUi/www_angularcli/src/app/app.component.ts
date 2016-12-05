import { Component, OnInit } from '@angular/core';
import { ErrorService } from './error.service';
import { Module } from './models';
import {
    Event as RouterEvent,
    NavigationStart,
    NavigationEnd,
    NavigationCancel,
    NavigationError, 
    Router
} from '@angular/router'
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent {
    title = 'Settop-box';
    error = '';
    // Sets initial value to true to show loading spinner on first load
    loading: boolean = true;
    
    constructor(private errorService : ErrorService, private router: Router) {
        if(environment.name){
          this.title = this.title + ' - ' + environment.name;
        }
        router.events.subscribe((event: RouterEvent) => {
            this.navigationInterceptor(event);
        });
    }
    
    ngOnInit(): void {
        this.errorService.errorOccured.subscribe(this.updateError);
        this.error = this.errorService.lastError;
        //this.errorService.subscribe(this.updateError);
    };

    // Shows and hides the loading spinner during RouterEvent changes
    navigationInterceptor(event: RouterEvent): void {
        if (event instanceof NavigationStart) {
            this.loading = true;
        }
        if (event instanceof NavigationEnd) {
            this.loading = false;
        }

        // Set loading state to false in both of the below events to hide the spinner in case a request fails
        if (event instanceof NavigationCancel) {
            this.loading = false;
        }
        if (event instanceof NavigationError) {
            this.loading = false;
        }
    }

    updateError(error: any): void {
      this.error = error.value;
    }
}
