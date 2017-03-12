import { Injectable } from '@angular/core';
import { Http } from '@angular/http'
import { ErrorService } from './error.service';
import { UrlsService } from './urls.service';

import { Channel } from './models'

@Injectable()
export class SettopboxService {

    constructor(private http: Http, private error: ErrorService, private urls: UrlsService) { }

    get(): Promise<Channel[]> {
        return this.http.get(this.urls.SettopBox)
            .toPromise()
            .then(response => {
                return response.json() as Channel[];
            })
            .catch(this.error.handleError);
    }

}
