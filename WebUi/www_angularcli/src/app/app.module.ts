import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';

import { ModuleService } from './module.service'
import { HomeService } from './home.service'
import { LogService } from './log.service'
import { SettingsService } from './settings.service'
import { UrlsService } from './urls.service'
import { ErrorService } from './error.service'
import { PerformanceService } from './performance.service'
import { SettopboxService } from './settopbox.service'

import { DashboardComponent } from './dashboard/dashboard.component';
import { WebUiComponent } from './webui/webui.component';
import { KeyblockComponent } from './keyblock/keyblock.component';
import { LogComponent } from './log/log.component';
import { NewcamdComponent } from './newcamd/newcamd.component';
import { EpgComponent } from './epg/epg.component';
import { TvheadendComponent } from './tvheadend/tvheadend.component';
import { SettingComponent } from './setting/setting.component';
import { ChannellistComponent } from './channellist/channellist.component';
import { ChannelComponent } from './channel/channel.component';
import { OverviewComponent } from './overview/overview.component';
import { ToggleButtonComponent } from './toggle-button/toggle-button.component';

@NgModule({
    declarations: [
        AppComponent,
        DashboardComponent,
        WebUiComponent,
        KeyblockComponent,
        LogComponent,
        NewcamdComponent,
        EpgComponent,
        TvheadendComponent,
        SettingComponent,
        ChannellistComponent,
        ChannelComponent,
        OverviewComponent,
        ToggleButtonComponent
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
        ErrorService,
        PerformanceService,
        SettopboxService
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
