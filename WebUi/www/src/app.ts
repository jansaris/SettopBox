import {HttpClient} from "aurelia-fetch-client";

export class App {
    static inject() { return [HttpClient]; }
    http: HttpClient;
    message: string;

    constructor(http: HttpClient) {
        http.configure(config => { config.withBaseUrl("/api/"); });
        this.http = http;
    }

    activate() {
        return this.http.fetch("home")
            .then(response => response.json()
            .then(mes => {
                this.message = mes;
        }));
    }
}
