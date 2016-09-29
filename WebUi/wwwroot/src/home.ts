import {HttpClient} from "aurelia-fetch-client";

export class Home {
    static inject() { return [HttpClient]; }
    message: string;
    modules: any;

    constructor(private http: HttpClient) {
        http.configure(config => { config.withBaseUrl("/api/"); });
        this.http = http;
    }

    getStatusClass(status: string): string {
        switch (status) {
            case "Running":
                return "alert-success";
            case "Disabled":
                return "alert-warning";
            default:
                return "alert-info";
        }
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
                    for (var index in this.modules) {
                        var module = modules[index];
                        module.class = this.getStatusClass(module.Status);
                    }
                }));
        return [home, list];
    }
}
