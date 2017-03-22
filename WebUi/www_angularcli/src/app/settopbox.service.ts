import { Injectable } from '@angular/core';
import { Http, Response } from '@angular/http'
import { ErrorService } from './error.service';
import { UrlsService } from './urls.service';

import { Channel, IptvInfo } from './models'

@Injectable()
export class SettopboxService {

    constructor(private http: Http, private error: ErrorService, private urls: UrlsService) { }

    get(): Promise<Channel[]> {
        return this.http
            .get(this.urls.SettopBox)
            .toPromise()
            .then(response => {
                return response.json() as Channel[];
            })
            .catch(this.error.handleError);
    }

    set(channel: Channel): Promise<Response> {
        return this.http
            .put(this.urls.SettopBox, channel)
            .toPromise()
            .catch(this.error.handleError);
    }

    iptvInfo(id: string): Promise<IptvInfo[]> {
        return this.http
            .get(this.urls.IptvInfo + id)
            .toPromise()
            .then(response => {
                return response.json() as IptvInfo[];
            })
            .catch(this.error.handleError);
    }

}
