import { NgModule }      from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule }   from '@angular/forms';
import { RouterModule }   from '@angular/router';
import { HttpModule }    from '@angular/http';

import { AppComponent }   from './app.component';
import { DashboardComponent }   from './dashboard.component';
import { WebUiComponent }   from './webui.component';


import { ModuleService } from './module.service'

import { AppRoutingModule }     from './app-routing.module';

@NgModule({
  imports:      [ BrowserModule, FormsModule, HttpModule, AppRoutingModule ],
  declarations: [ AppComponent, DashboardComponent, WebUiComponent ],
  bootstrap:    [ AppComponent ],
   providers: [
    ModuleService
  ]
})
export class AppModule { }