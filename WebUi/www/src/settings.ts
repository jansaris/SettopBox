import {HttpClient} from "aurelia-fetch-client";

export class Settings {
    static inject() { return [HttpClient]; }
    settings: ISetting[];
    module: string;

    constructor(private http: HttpClient) {
        http.configure(config => { config.withBaseUrl("/api/"); });
        this.http = http;
    }

    activate(params, routeConfig) {
        this.module = params.moduleId;
        return this.http.fetch(`settings/?module=${this.module}`)
            .then(response => response.json()
                .then(settings => {
                    this.settings = <ISetting[]>settings;
                    for (var index in this.settings) {
                        var set = this.settings[index];
                        switch (set.Type) {
                            case "Boolean":
                                set.InputType = "checkbox";
                                break;
                            case "Int32":
                            case "Double":
                                set.InputType = "number";
                                break;
                            default:
                                set.InputType = "text";
                                break;
                        }
                    }
                }));
    }
}
