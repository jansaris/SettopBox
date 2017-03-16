import { Component, OnInit, Input  } from '@angular/core';
import { Channel, ChannelLocations } from '../models';

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
    orgKeyblock: boolean;
    changes: number;

    constructor() {
        
    }

    ngOnInit() {
        this.orgEpg = this.info.EpgGrabber;
        this.orgTvh = this.info.TvHeadend;
        this.orgKeyblock = this.info.Keyblock;
    }

    calculate() {
      
    }

    getNrOfChangedSettings(): number {
        var count = 0;
        if (this.orgEpg != this.info.EpgGrabber) count = count + 1;
        if (this.orgTvh != this.info.TvHeadend) count = count + 1;
        if (this.orgKeyblock != this.info.Keyblock) count = count + 1;
        return count;
    }

    onToggleButtonChange(name: string, enabled: boolean) {
        switch (name) {
            case 'EPG': this.info.EpgGrabber = enabled; break;
            case 'Keyblock': this.info.Keyblock = enabled; break;
            case 'TvHeadend': this.info.TvHeadend = enabled; break;
        }
    }

    saveSettings() {

    }

    resetSettings() {
        this.info.EpgGrabber = this.orgEpg;
        this.info.TvHeadend = this.orgTvh;
        this.info.Keyblock = this.orgKeyblock;
    }
}
