import { Component, OnInit } from '@angular/core';
import { ErrorService } from './error.service';
import { Module } from './models';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent {
    title: string = 'Settop-box';
    error: string = '';
    // Sets initial value to true to show loading spinner on first load
    loading: boolean = true;
    
    constructor(private errorService : ErrorService) {
        if(environment.name){
            this.title = this.title + ' - ' + environment.name;
        }
    }
    
    ngOnInit(): void {
        this.errorService.errorOccured.subscribe(this.updateError);
        this.error = this.errorService.lastError;
        //this.errorService.subscribe(this.updateError);
    };

    updateError(error: any): void {
      this.error = error.value;
    }
}
