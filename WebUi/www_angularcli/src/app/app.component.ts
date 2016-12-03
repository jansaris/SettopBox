import { Component, OnInit } from '@angular/core';
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
    
    constructor(private errorService : ErrorService) { }
    
    ngOnInit(): void {
        this.errorService.errorOccured.subscribe(this.updateError);
        this.error = this.errorService.lastError;
        //this.errorService.subscribe(this.updateError);
    };

    updateError(error: any): void {
      this.error = error.value;
    }
}
