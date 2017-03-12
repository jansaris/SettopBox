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

    constructor() {
    }

    ngOnInit() {
    }

}
