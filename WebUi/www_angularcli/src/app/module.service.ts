import { Injectable } from '@angular/core';
import { Headers, Http, URLSearchParams } from '@angular/http'

import 'rxjs/add/operator/toPromise';
import './models'

@Injectable()
export class ModuleService {
    private moduleUrl = 'http://localhost:15051/api/module';  // URL to web api
    private headers = new Headers([ {'Content-Type': 'application/json'}]);

    constructor(private http: Http) { }
    
    getModules(): Promise<IModule[]> {
        return this.http.get(this.moduleUrl)
               .toPromise()
               .then(response => {
                   return response.json() as IModule[];
                })
               .catch(this.handleError);
    }

    getModule(name: string): Promise<IModule> {
        let params: URLSearchParams = new URLSearchParams();
        params.set('name', name);

        return this.http.get(this.moduleUrl, {search: params})
                        .toPromise()
                        .then(response => {
                            return response.json() as IModule;
                        })
        .catch(this.handleError);
    }

    private handleError(error: any): Promise<any> {
        console.error('An error occurred', error); // for demo purposes only
        return Promise.reject(error.message || error);
    }
}