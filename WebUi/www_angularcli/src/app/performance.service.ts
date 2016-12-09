import { Injectable } from '@angular/core';
import { Http } from '@angular/http'
import { ErrorService } from './error.service';
import { UrlsService } from './urls.service';

import { Performance } from './models'

@Injectable()
export class PerformanceService {

  constructor(private http: Http, private error: ErrorService, private urls: UrlsService) { }

  get(): Promise<Performance> {
      return this.http.get(this.urls.Performance)
              .toPromise()
              .then(response => {
                  return response.json() as Performance;
              })
              .catch(this.error.handleError);
  }

}
