import { Component } from '@angular/core';
import { ModuleService } from '../module.service'
import { SettingsService } from '../settings.service'
import { ModuleBaseComponent } from '../module.base.component'
import { SettingComponent } from '../setting/setting.component'

import { Module, ChannelListInfo, ChannelInfo } from '../models';

@Component({
  selector: 'app-channellist',
  templateUrl: './channellist.component.html',
  styleUrls: ['./channellist.component.css']
})
export class ChannellistComponent extends ModuleBaseComponent {

  apiName: string = "ChannelList";
  info: ChannelListInfo;

  constructor(moduleService: ModuleService, settingsService: SettingsService) {
    super(moduleService, settingsService);
    this.info = new ChannelListInfo();
  }

  updateInfo(m: Module): void {
    var typedInfo = m.Info as ChannelListInfo;
    if(typedInfo.LastRetrieval) this.info.LastRetrieval = typedInfo.LastRetrieval;
    if(typedInfo.State) this.info.State = typedInfo.State;
    if(typedInfo.Channels) this.info.Channels = typedInfo.Channels;
  }

  loadData(): void {
    this.loading = true;
    this.moduleService.data(this.apiName).then(m => this.updateModule(m));
    super.loadData();
  }

  showDetails(channel: ChannelInfo): void{
    if(channel.DetailsTimer) return;
    if(channel.Locations.length <= 1) return; 
    channel.DetailsVisible = true;
  }

  hideDetails(channel: ChannelInfo): void{
    channel.DetailsVisible = false;
    channel.DetailsTimer = setTimeout(() => channel.DetailsTimer = 0, 50);
  }

}
