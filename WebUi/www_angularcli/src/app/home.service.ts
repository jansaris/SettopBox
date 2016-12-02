import { Injectable } from '@angular/core';
import { Http  } from '@angular/http'

import { UrlsService } from './urls.service'
import { ErrorService } from './error.service'

import 'rxjs/add/operator/toPromise';

@Injectable()
export class HomeService {

  constructor(private http: Http, private error: ErrorService, private urls: UrlsService) { }

  public get(): Promise<string>{
    return this.http.get(this.urls.Home)
               .toPromise()
               .then(response => {
                   return response.json() as string;
                })
               .catch(this.error.handleError);
  }
}
