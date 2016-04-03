import {HttpClient} from "aurelia-fetch-client";

export class Home {
    static inject() { return [HttpClient]; }
    http: HttpClient;
    message: string;
    modules: any;

    constructor(http: HttpClient) {
        http.configure(config => { config.withBaseUrl("/api/"); });
        this.http = http;
    }

    activate() {
        var home = this.http.fetch("home")
            .then(response => response.json()
            .then(mes => {
                this.message = mes;
                }));
        var list = this.http.fetch("module")
            .then(response => response.json()
                .then(modules => {
                    this.modules = modules;
                }));
        return [home, list];
    }
}
