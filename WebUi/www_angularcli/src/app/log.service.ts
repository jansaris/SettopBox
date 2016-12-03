import { Injectable } from '@angular/core';
import { Http, URLSearchParams } from '@angular/http'
import { ErrorService } from './error.service';
import { UrlsService } from './urls.service';

import { Log } from './models'

import 'rxjs/add/operator/toPromise';

@Injectable()
export class LogService {

  constructor(private http: Http, private error: ErrorService, private urls: UrlsService) { }

  getLevels(): Promise<string[]>{
        let url = this.urls.Log + '/levels';

        return this.http.get(url)
                .toPromise()
                .then(response => {
                    return response.json() as string[];
                })
                .catch(this.error.handleError);
  }

  getLog(module: string, level: string): Promise<Log[]>{
    let params: URLSearchParams = new URLSearchParams();
        params.set('module', module);
        params.set('level', level);

    return this.http.get(this.urls.Log, {search: params})
                .toPromise()
                .then(response => {
                    return response.json() as string[];
                })
                .catch(this.error.handleError);
  }
}
