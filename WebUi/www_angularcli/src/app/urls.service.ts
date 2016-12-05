import { Injectable } from '@angular/core';
import { Headers } from '@angular/http'
import { environment } from '../environments/environment';

@Injectable()
export class UrlsService{
    private base: string = environment.apiBaseUrl;
    Home: string = this.base + "home";
    Log: string = this.base + "logging";
    Settings: string = this.base + "settings";
    Module: string = this.base + "module";
    Headers: Headers = new Headers([ {'Content-Type': 'application/json'}]);
}