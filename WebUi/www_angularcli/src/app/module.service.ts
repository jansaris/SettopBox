import { Injectable } from '@angular/core';
import { Http, URLSearchParams } from '@angular/http'
import { ErrorService } from './error.service';
import { UrlsService } from './urls.service';

import {Module} from './models'

import 'rxjs/add/operator/toPromise';

@Injectable()
export class ModuleService {
    constructor(private http: Http, private error: ErrorService, private urls: UrlsService) { }
    
    getAll(): Promise<Module[]> {
        return this.http.get(this.urls.Module)
               .toPromise()
               .then(response => {
                   return response.json() as Module[];
                })
               .catch(this.error.handleError);
    }

    getNames(): Promise<string[]> {
        let url = this.urls.Module + '/names';

        return this.http.get(url)
                .toPromise()
                .then(response => {
                    return response.json() as string[];
                })
                .catch(this.error.handleError);
    }

    get(name: string): Promise<Module> {
        let params: URLSearchParams = new URLSearchParams();
        params.set('name', name);

        return this.http.get(this.urls.Module, {search: params})
                .toPromise()
                .then(response => {
                    return response.json() as Module;
                })
                .catch(this.error.handleError);
    }

    data(name: string): Promise<Module> {
        return this.http.get(this.urls.Module + '/data/' + name, null)
                .toPromise()
                .then(response => {
                    return response.json() as Module;
                })
                .catch(this.error.handleError);
    }

    start(name: string): Promise<Module>{
        return this.http.post(this.urls.Module + '/start/' + name, null)
                .toPromise()
                .then(response => {
                    return response.json() as Module;
                })
                .catch(this.error.handleError);
    }

    stop(name: string): Promise<Module>{
         let params: URLSearchParams = new URLSearchParams();
        params.set('name', name);

        return this.http.post(this.urls.Module + '/stop/' + name, null)
        //return this.http.get(this.urls.Module + '/stop', {search: params})
                .toPromise()
                .then(response => {
                    return response.json() as Module;
                })
                .catch(this.error.handleError);
    }
}