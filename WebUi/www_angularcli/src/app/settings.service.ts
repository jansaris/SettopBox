import { Injectable } from '@angular/core';
import { Headers, Http, URLSearchParams } from '@angular/http'
import { ErrorService } from './error.service';
import { UrlsService } from './urls.service';

import { Setting } from './models'

import 'rxjs/add/operator/toPromise';

@Injectable()
export class SettingsService {

  constructor(private http: Http, private error: ErrorService, private urls: UrlsService) { }

  get(name: string): Promise<Setting[]> {
    let params: URLSearchParams = new URLSearchParams();
    params.set('module', name);

    return this.http.get(this.urls.Settings, { search: params })
      .toPromise()
      .then(response => {
        return response.json() as Setting[];
      })
      .then(this.addSettingType)
      .then(this.saveServerValue)
      .catch(this.error.handleError);
  }

  addSettingType(settings: Setting[]) : Setting[] {
    for (var index in settings) {
          var set = settings[index];
          switch (set.Type) {
              case "Boolean":
                  set.InputType = "checkbox";
                  break;
              case "Int32":
              case "Double":
                  set.InputType = "number";
                  break;
              default:
                  set.InputType = "text";
                  break;
          }
      }
      return settings;
  }

  saveServerValue(settings: Setting[]) : Setting[]{
      for (var index in settings) {
          settings[index].ServerValue = settings[index].Value;
      }
      return settings;
  }
}
