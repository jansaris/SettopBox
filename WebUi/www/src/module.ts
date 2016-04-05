import {HttpClient} from "aurelia-fetch-client";

export class Module {
    static inject() { return [HttpClient]; }
    message: string;
    module: IModule;

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

    activate(params, routeConfig) {
        return this.http.fetch("module/?name=" + params.id)
            .then(response => response.json()
            .then(moduleFromResponse => {
                this.module = <IModule>moduleFromResponse;
            }));
    }
}
