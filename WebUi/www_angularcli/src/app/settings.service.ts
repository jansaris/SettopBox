import { Injectable } from '@angular/core';
import { Headers, Http, URLSearchParams } from '@angular/http'
import { ErrorService } from './error.service';


@Injectable()
export class SettingsService {

  constructor(private http: Http, private error: ErrorService) { }

}
