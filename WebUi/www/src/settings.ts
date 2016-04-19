import {HttpClient} from "aurelia-fetch-client";

export class Settings {
    static inject() { return [HttpClient]; }
    settings: ISetting[];

    constructor(private http: HttpClient, private module: string) {
        http.configure(config => { config.withBaseUrl("/api/"); });
        this.http = http;
    }

    activate(params, routeConfig) {
        return this.http.fetch("settings/?module=" + params.id)
            .then(response => response.json()
            .then(settings => {
                this.settings = <ISetting[]>settings;
            }));
    }
}
