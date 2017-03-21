import { Component, OnInit, Input  } from '@angular/core';
import { Channel, ChannelLocations } from '../models';
import { SettopboxService } from '../settopbox.service';

@Component({
    selector: 'channel',
    templateUrl: './channel.component.html',
    styleUrls: ['./channel.component.css']
})
export class ChannelComponent implements OnInit {
    @Input()
    info: Channel;
    orgEpg: boolean;
    orgTvh: boolean;
    orgChannel: string;
    orgKeyblock: boolean;
    changes: number;
    loading: boolean;
    TvHeadendLoading: boolean;

    constructor(private settopboxService: SettopboxService) {
        
    }

    ngOnInit() {
        this.updateOriginals(this.info);
    }

    updateOriginals(channel: Channel) {
        this.orgEpg = channel.EpgGrabber;
        this.orgTvh = channel.TvHeadend;
        this.orgKeyblock = channel.Keyblock;
        this.orgChannel = channel.TvHeadendChannel;
    }

    getNrOfChangedSettings(): number {
        var count = 0;
        if (this.orgEpg != this.info.EpgGrabber) count = count + 1;
        if (this.orgTvh != this.info.TvHeadend) count = count + 1;
        if (this.orgKeyblock != this.info.Keyblock) count = count + 1;
        if (this.orgChannel != this.info.TvHeadendChannel) count = count + 1;
        return count;
    }

    onToggleButtonChange(name: string, enabled: boolean) {
        switch (name) {
            case 'EPG': this.info.EpgGrabber = enabled; break;
            case 'Keyblock': this.info.Keyblock = enabled; break;
            case 'TvHeadend': this.toggleTvHeadend(enabled); break;
        }
    }

    toggleTvHeadend(enabled) {
        this.info.TvHeadend = enabled;
        if (!enabled) return;
        this.TvHeadendLoading = true;
        this.settopboxService
            .iptvInfo(this.info.Id)
            .then(r => {
                this.TvHeadendLoading = false;
            }).catch(r => {
                this.TvHeadendLoading = false;
            });

    }

    saveSettings() {
        this.loading = true;
        this.settopboxService
            .set(this.info)
            .then(r => {
                this.updateOriginals(this.info);
                this.loading = false;
            }).catch(r => {
                this.loading = false;
            });
    }

    resetSettings() {
        this.info.EpgGrabber = this.orgEpg;
        this.info.TvHeadend = this.orgTvh;
        this.info.Keyblock = this.orgKeyblock;
        this.info.TvHeadendChannel = this.orgChannel;
    }
}
