import {HttpClient} from "aurelia-fetch-client";

export class NewCamd {
    static inject() { return [HttpClient]; }
    module: IModule;
    info: INewcamdInfo;

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
        return this.loadInfo();
    }

    loadInfo() {
        return this.http.fetch("module/?name=NewCamd")
            .then(response => response.json()
                .then(moduleFromResponse => {
                    this.module = <IModule>moduleFromResponse;
                    this.info = <INewcamdInfo>this.module.Info;
                }));
    }
}
