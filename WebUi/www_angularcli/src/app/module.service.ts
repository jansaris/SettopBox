import { Injectable } from '@angular/core';
import { Http, URLSearchParams } from '@angular/http'
import { ErrorService } from './error.service';
import { UrlsService } from './urls.service';

import {Module} from './models'

import 'rxjs/add/operator/toPromise';

@Injectable()
export class ModuleService {
    constructor(private http: Http, private error: ErrorService, private urls: UrlsService) { }
    
    getModules(): Promise<Module[]> {
        return this.http.get(this.urls.Module)
               .toPromise()
               .then(response => {
                   return response.json() as Module[];
                })
               .catch(this.error.handleError);
    }

    getModule(name: string): Promise<Module> {
        let params: URLSearchParams = new URLSearchParams();
        params.set('name', name);

        return this.http.get(this.urls.Module, {search: params})
                        .toPromise()
                        .then(response => {
                            return response.json() as Module;
                        })
        .catch(this.error.handleError);
    }
}