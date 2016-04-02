import {HttpClient} from "aurelia-fetch-client";

export class App {
    static inject() { return [HttpClient]; }
    http: HttpClient;
    message: string;

    constructor(http: HttpClient) {
        http.configure(config => { config.withBaseUrl("/"); });
        this.http = http;
    }

    activate() {
        return this.http.fetch("welcome")
            .then(response => response.text()
            .then(mes => {
                this.message = mes;
        }));
    }
}
