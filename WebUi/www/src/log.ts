import {HttpClient} from "aurelia-fetch-client";

export class Keyblock {
    static inject() { return [HttpClient]; }
    list: ILog[];
    levels: string[];
    modules: string[];
    all = "ALL";
    activeLevel = this.all;
    activeModule = this.all;

    constructor(private http: HttpClient) {
        http.configure(config => { config.withBaseUrl("/api/"); });
        this.http = http;
    }

    activate() {
        var listRequest = this.loadLog();
        var levelsRequest = this.http.fetch(`logging/levels`)
            .then(response => response.json()
                .then(list => {
                    this.levels = <string[]>list;
                }));
        var moduleRequest = this.http.fetch(`module/names`)
            .then(response => response.json()
                .then(list => {
                    this.modules = <string[]>list;
                }));
        return [listRequest, levelsRequest, moduleRequest];
    }

    changeLevel(level: string) {
        this.activeLevel = level;
        this.loadLog();
    }

    changeModule(module: string) {
        this.activeModule = module;
        this.loadLog();
    }

    loadLog() {
        return this.http.fetch(`logging?module=${this.activeModule}&level=${this.activeLevel}`)
            .then(response => response.json()
                .then(logList => {
                    this.list = <ILog[]>logList;
                }));
    }
}
