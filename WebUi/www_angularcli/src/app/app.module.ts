import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';

import { AppComponent } from './app.component';
import { AppRoutingModule }     from './app-routing.module';

import { ModuleService } from './module.service'
import { HomeService } from './home.service'
import { LogService } from './log.service'
import { SettingsService } from './settings.service'
import { UrlsService } from './urls.service'
import { ErrorService } from './error.service'

import { DashboardComponent } from './dashboard/dashboard.component';
import { WebUiComponent } from './webui/webui.component';
import { KeyblockComponent } from './keyblock/keyblock.component';
import { LogComponent } from './log/log.component';
import { NewcamdComponent } from './newcamd/newcamd.component';
import { EpgComponent } from './epg/epg.component';
import { TvheadendComponent } from './tvheadend/tvheadend.component';

@NgModule({
  declarations: [
    AppComponent,
    DashboardComponent,
    WebUiComponent,
    KeyblockComponent,
    LogComponent,
    NewcamdComponent,
    EpgComponent,
    TvheadendComponent
  ],
  imports: [
    BrowserModule,
    FormsModule,
    HttpModule,
    AppRoutingModule
  ],
  providers: [
    ModuleService,
    HomeService,
    LogService,
    SettingsService,
    UrlsService,
    ErrorService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }