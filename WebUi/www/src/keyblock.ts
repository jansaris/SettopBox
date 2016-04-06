import {HttpClient} from "aurelia-fetch-client";

export class Keyblock {
    static inject() { return [HttpClient]; }
    module: IModule;
    info: IKeyblockInfo;
    panelColor: string;

    constructor(private http: HttpClient) {
        http.configure(config => { config.withBaseUrl("/api/"); });
        this.http = http;
    }

    getStatusClass(status: string): string {
        switch (status) {
            case "Running":
                return "panel-success";
            case "Disabled":
                return "panel-warning";
            default:
                return "panel-info";
        }
    }

    activate() {
        return this.loadInfo();
    }

    loadInfo() {
        return this.http.fetch("module/?name=Keyblock")
            .then(response => response.json()
                .then(moduleFromResponse => {
                    this.module = <IModule>moduleFromResponse;
                    this.info = <IKeyblockInfo>this.module.Info;
                    this.panelColor = this.getStatusClass(this.module.Status);
                }));
    }
}
