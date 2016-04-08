import {HttpClient} from "aurelia-fetch-client";

export class Keyblock {
    static inject() { return [HttpClient]; }
    list: ILog[];

    constructor(private http: HttpClient) {
        http.configure(config => { config.withBaseUrl("/api/"); });
        this.http = http;
    }

    activate() {
        return this.loadLog();
    }

    loadLog() {
        return this.http.fetch("logging")
            .then(response => response.json()
                .then(logList => {
                    this.list = <ILog[]>logList;
                }));
    }
}
